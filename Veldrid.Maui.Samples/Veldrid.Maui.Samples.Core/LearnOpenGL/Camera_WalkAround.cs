﻿using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid.Maui.Controls.AssetPrimitives;
using Veldrid.Maui.Controls.Base;
using Veldrid.SPIRV;

namespace Veldrid.Maui.Samples.Core.LearnOpenGL
{
    public class Camera_WalkAround : BaseGpuDrawable
    {
        private DeviceBuffer _vertexBuffer;
        private Pipeline _pipeline;
        private CommandList _commandList;
        private ResourceSet _transSet;
        private ResourceSet _textureSet;
        private Shader[] _shaders;
        private DeviceBuffer _indexBuffer;
        private DeviceBuffer _instanceVB;
        private ProcessedTexture texture1;
        private Texture _surfaceTexture1;
        private TextureView _surfaceTextureView1;
        private ProcessedTexture texture2;
        private Texture _surfaceTexture2;
        private TextureView _surfaceTextureView2;
        private DeviceBuffer _modelBuffer;
        private DeviceBuffer _viewBuffer;
        private DeviceBuffer _projectionBuffer;
        private float[] vertices;
        private ushort[] indices;

        [StructLayout(LayoutKind.Sequential)]
        struct VerticeData
        {
            Vector3 Position;
            Vector2 TextureCoord;

            public VerticeData(float x, float y, float z, float tx, float ty)
            {
                Position = new Vector3(x, y, z);
                TextureCoord = new Vector2(tx, ty);
            }
        }

        protected unsafe override void CreateResources(ResourceFactory factory)
        {
            vertices = vertices0;
            indices = indices0;
            // create Buffer for vertices data
            BufferDescription vertexBufferDescription = new BufferDescription(
                (uint)(vertices.Length / 5f * sizeof(VerticeData)),
                BufferUsage.VertexBuffer);
            _vertexBuffer = factory.CreateBuffer(vertexBufferDescription);
            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices);// update data to Buffer

            // create IndexBuffer
            BufferDescription indexBufferDescription = new BufferDescription(
                (uint)(indices.Length * sizeof(ushort)),
                BufferUsage.IndexBuffer);
            _indexBuffer = factory.CreateBuffer(indexBufferDescription);
            GraphicsDevice.UpdateBuffer(_indexBuffer, 0, indices);// update data to Buffer

            string vertexCode = @"
#version 450

layout(set = 0, binding = 0) uniform ModelTrans
{
  mat4 model;
};
layout(set = 0, binding = 1) uniform ViewTrans
{
  mat4 view;
};
layout(set = 0, binding = 2) uniform ProjectionTrans
{
  mat4 projection;
};

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 InstancePosition;

layout (location = 0) out vec2 TexCoord;

void main()
{
    mat4 delta = mat4(vec4(1.0, 0.0, 0.0, 0.0),vec4(0.0, 1.0, 0.0, 0.0),vec4(0.0, 0.0, 1.0, 0.0),vec4(InstancePosition, 1.0));
    // note that we read the multiplication from right to left
    gl_Position = projection * view *  delta * model * vec4(aPos, 1.0);
    TexCoord = vec2(aTexCoord.x, 1 - aTexCoord.y);
}";

            string fragmentCode = @"
#version 450

layout(location = 0) out vec4 FragColor;
layout(location = 0) in vec2 TexCoord;

layout(set = 1, binding = 0) uniform texture2D Texture1;//In veldrid, use 'uniform' to input something need 'set' descriptor 
layout(set = 1, binding = 1) uniform texture2D Texture2;
layout(set = 1, binding = 2) uniform sampler Sampler;

void main()
{
    FragColor = mix(texture(sampler2D(Texture1, Sampler), TexCoord),texture(sampler2D(Texture2, Sampler),TexCoord) ,0.2);
}";
            var vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main");
            var fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main");

            // OpenGL no layout(set), so we need use Spirv to convert
            /*if (factory.BackendType == GraphicsBackend.OpenGL)
            {
                var vertexShader = factory.CreateShader(vertexShaderDesc);
                var fragmentShader = factory.CreateShader(fragmentShaderDesc);
                _shaders = new Shader[] { vertexShader, fragmentShader };
            }
            else*/
            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            // VertexLayout tell Veldrid we store wnat in Vertex Buffer, it need match with vertex.glsl
            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
               new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
               new VertexElementDescription("TextureCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

            VertexLayoutDescription vertexLayoutPerInstance = new VertexLayoutDescription(
               new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));
            vertexLayoutPerInstance.InstanceStepRate = 1;
            _instanceVB = ResourceFactory.CreateBuffer(new BufferDescription((uint)(sizeof(Vector3) * cubePositions.Length / 3), BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_instanceVB, 0, cubePositions);

            // from file load image as texture
            //texture1 = new ImageProcessor().ProcessT(this.ReadEmbedAssetStream("LearnOpenGL.Assets.Images.container.jpg"), ".jpg");
            texture1 = LoadEmbeddedAsset<ProcessedTexture>(this.ReadEmbedAssetPath("ProcessedImages.container.binary"));
            _surfaceTexture1 = texture1.CreateDeviceTexture(GraphicsDevice, ResourceFactory, TextureUsage.Sampled);
            _surfaceTextureView1 = factory.CreateTextureView(_surfaceTexture1);
            //texture2 = new ImageProcessor().ProcessT(this.ReadEmbedAssetStream("LearnOpenGL.Assets.Images.awesomeface.png"), ".png");
            texture2 = LoadEmbeddedAsset<ProcessedTexture>(this.ReadEmbedAssetPath("ProcessedImages.awesomeface.binary"));
            _surfaceTexture2 = texture2.CreateDeviceTexture(GraphicsDevice, ResourceFactory, TextureUsage.Sampled);
            _surfaceTextureView2 = factory.CreateTextureView(_surfaceTexture2);

            _modelBuffer = factory.CreateBuffer(new BufferDescription((uint)sizeof(Matrix4x4), BufferUsage.UniformBuffer));
            _viewBuffer = factory.CreateBuffer(new BufferDescription((uint)sizeof(Matrix4x4), BufferUsage.UniformBuffer));
            _projectionBuffer = factory.CreateBuffer(new BufferDescription((uint)sizeof(Matrix4x4), BufferUsage.UniformBuffer));
            ResourceLayout transLayout = factory.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("ModelTrans", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                   new ResourceLayoutElementDescription("ViewTrans", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                   new ResourceLayoutElementDescription("ProjectionTrans", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                   ));

            ResourceLayout textureLayout = factory.CreateResourceLayout(
                 new ResourceLayoutDescription(
                     new ResourceLayoutElementDescription("Texture1", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                     new ResourceLayoutElementDescription("Texture2", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                     new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
                    ));

            // create GraphicsPipeline
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual);
            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.None,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                );//not cull face
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;//basis graphics is point,line,or triangle
            pipelineDescription.ResourceLayouts = new[] { transLayout, textureLayout };
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout, vertexLayoutPerInstance },
                shaders: _shaders);
            pipelineDescription.Outputs = MainSwapchain.Framebuffer.OutputDescription;

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            // create CommandList
            _commandList = factory.CreateCommandList();
            // create ResourceSet for transform
            _transSet = factory.CreateResourceSet(new ResourceSetDescription(
              transLayout,
              _modelBuffer,
              _viewBuffer,
              _projectionBuffer
              ));
            // create ResourceSet for texture
            _textureSet = factory.CreateResourceSet(new ResourceSetDescription(
               textureLayout,
               _surfaceTextureView1,
               _surfaceTextureView2,
               GraphicsDevice.LinearSampler
               ));
        }

        #region Walk around
        Vector3 cameraPos = new Vector3(0.0f, 0.0f, 3.0f);
        Vector3 cameraFront = new Vector3(0.0f, 0.0f, -1.0f);
        Vector3 cameraUp = new Vector3(0.0f, 1.0f, 0.0f);
        public void processInput(char key)
        {
            float cameraSpeed = 2.5f; // adjust accordingly
            if (key == 'W')
                cameraPos += cameraSpeed * cameraFront;
            if (key == 'S')
                cameraPos -= cameraSpeed * cameraFront;
            if (key == 'A')
                cameraPos -= Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp)) * cameraSpeed;
            if (key == 'D')
                cameraPos += Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp)) * cameraSpeed;
        }
        #endregion

        #region Mouse input
        bool firstMouse = true;
        float lastX;
        float lastY;
        float yaw;
        float pitch;
        void mouse_callback(float xpos, float ypos)
        {
            if (firstMouse)
            {
                lastX = xpos;
                lastY = ypos;
                firstMouse = false;
            }

            float xoffset = xpos - lastX;
            float yoffset = lastY - ypos;
            lastX = xpos;
            lastY = ypos;

            float sensitivity = 0.05f;
            xoffset *= sensitivity;
            yoffset *= sensitivity;

            yaw += xoffset;
            pitch += yoffset;

            if (pitch > 89.0f)
                pitch = 89.0f;
            if (pitch < -89.0f)
                pitch = -89.0f;

            Vector3 front;
            front.X = MathF.Cos((yaw)) * MathF.Cos((pitch));
            front.Y = MathF.Sin((pitch));
            front.Z = MathF.Sin((yaw)) * MathF.Cos((pitch));
            cameraFront = Vector3.Normalize(front);
        }
        #endregion

        #region Zoom
        float fov = 45;
        void scroll_callback(float xoffset, float yoffset)
        {
            if (fov >= 1.0f && fov <= 45.0f)
                fov -= yoffset;
            if (fov <= 1.0f)
                fov = 1.0f;
            if (fov >= 45.0f)
                fov = 45.0f;
        }
        #endregion

        private float _ticks;
        protected override void Draw(float deltaSeconds)
        {
            // Begin() must be called before commands can be issued.
            _commandList.Begin();

            _commandList.SetFramebuffer(MainSwapchain.Framebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            _commandList.ClearDepthStencil(1f);

            _ticks += deltaSeconds;
            var angle = _ticks / 1000 * MathF.PI / 180 * 50;
            var model = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, angle)
                * Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, angle);

            var view = Matrix4x4.CreateLookAt(cameraPos, cameraPos + cameraFront, cameraUp);

            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 180 * fov,
                PlatformInterface.Width / (float)PlatformInterface.Height,
                0.1f,
                100f);
            _commandList.UpdateBuffer(_modelBuffer, 0, model);
            _commandList.UpdateBuffer(_viewBuffer, 0, view);
            _commandList.UpdateBuffer(_projectionBuffer, 0, projection);

            _commandList.SetPipeline(_pipeline);

            _commandList.SetGraphicsResourceSet(0, _transSet);
            _commandList.SetGraphicsResourceSet(1, _textureSet);

            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.SetVertexBuffer(1, _instanceVB);

            _commandList.DrawIndexed(
                indexCount: (uint)indices.Length,
                instanceCount: (uint)(cubePositions.Length / 3),
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // End() must be called before commands can be submitted for execution.
            _commandList.End();
            GraphicsDevice?.SubmitCommands(_commandList);
            // Once commands have been submitted, the rendered image can be presented to the application window.
            GraphicsDevice?.SwapBuffers(MainSwapchain);
        }

        float[] vertices0 = {
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,//back and right bottom triangle
             0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,//back and left top tria
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,

            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,//front and right bottom tria
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,

            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,

            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f
        };

        ushort[] indices0 =
        {
            0,1,2, 3,4,5,
            6,7,8, 9,10,11,
            12,13,14, 15,16,17,
            18,19,20, 21,22,23,
            24,25,26, 27,28,29,
            30,31,32, 33,34,35
        };

        float[] cubePositions = {
           0.0f,  0.0f,  0.0f,
           2.0f,  5.0f, -15.0f,
          -1.5f, -2.2f, -2.5f,
          -3.8f, -2.0f, -12.3f,
           2.4f, -0.4f, -3.5f,
          -1.7f,  3.0f, -7.5f,
           1.3f, -2.0f, -2.5f,
           1.5f,  2.0f, -2.5f,
           1.5f,  0.2f, -1.5f,
          -1.3f,  1.0f, -1.5f
        };
    }
}
