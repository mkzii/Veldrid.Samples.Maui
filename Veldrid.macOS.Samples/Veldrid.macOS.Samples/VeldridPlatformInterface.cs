﻿using CoreVideo;
using System.Diagnostics;
using Veldrid.Maui.Controls.Base;
using Veldrid.Utilities;

namespace Veldrid.macOS.Samples
{
    public class VeldridPlatformInterface : //UIViewController, 
        BaseVeldridPlatformInterface
    {
        private VeldridPlatformView _view;

        private readonly GraphicsDeviceOptions _options;
        private readonly GraphicsBackend _backend;
        private CVDisplayLink _timer;

        public override uint Width => (uint)(_view.CorrectedFrame.Width * NSScreen.MainScreen.BackingScaleFactor);
        public override uint Height => (uint)(_view.CorrectedFrame.Height * NSScreen.MainScreen.BackingScaleFactor);

        public override bool AutoReDraw { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public VeldridPlatformInterface(VeldridPlatformView view, GraphicsBackend backend = GraphicsBackend.Metal)
        {
            PlatformType = PlatformType.Desktop;

            if (!(backend == GraphicsBackend.Metal || backend == GraphicsBackend.Vulkan))
                throw new NotSupportedException($"Not support {backend} backend on Mac.");
            _backend = backend;

            _options = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved, true, true);

            _view = view;
            _view.ViewLoaded += CreateGraphicsDevice;
            _view.SizeChanged += OnViewSizeChanged;
            _view.ViewRemoved += DestroyGraphicsDevice;
        }

        /// <summary>
        /// 在<see cref="CreateGraphicsDevice"/>后自动执行, 开使渲染循环.
        /// </summary>
        public void Run()
        {
            // To render at a steady rate, we create a display link which will invoke our Render function.
            _timer = new CVDisplayLink();
            _timer.SetOutputCallback(RenderLoop);
            /*
            if (DeviceInfo.Current.Version < new Version(10, 0))
            {
                _timer.FrameInterval = 1;
            }
            else if (DeviceInfo.Current.Version < new Version(15, 0))
            {
                _timer.PreferredFramesPerSecond = 60;
            }
            else
            {
                _timer.PreferredFrameRateRange = CAFrameRateRange.Default;
            }*/
            _timer.Start();
        }

        private CVReturn RenderLoop(
            CVDisplayLink displayLink,
            ref CVTimeStamp inNow,
            ref CVTimeStamp inOutputTime,
            CVOptionFlags flagsIn,
            ref CVOptionFlags flagsOut)
        {
            float elapsed = 16;
            if (_graphicsDevice != null)
            {
                try
                {
                    InvokeRendering(elapsed);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Encountered an error while rendering: " + e);
                    //throw;
                }
            }
            return CVReturn.Success;
        }

        private void DestroyGraphicsDevice()
        {
            if (_graphicsDevice != null)
            {
                var tempDevice = _graphicsDevice;
                _graphicsDevice = null;//先设置null阻止渲染循环

                InvokeGraphicsDeviceDestroyed();
                tempDevice.WaitForIdle();
                (_resources as DisposeCollectorResourceFactory)?.DisposeCollector.DisposeAll();
                tempDevice.Dispose();
            }
        }

        private void CreateGraphicsDevice()
        {
            SwapchainSource ss = SwapchainSource.CreateNSView(_view.Handle);
            //MTLSwapchain内部自动转换成Pixel
            SwapchainDescription scd = new SwapchainDescription(ss,(uint)_view.CorrectedFrame.Width,(uint)_view.CorrectedFrame.Height, PixelFormat.R32_Float,false);//参考Veldrid.Samples的iOS
            //SwapchainDescription scd = new SwapchainDescription(ss, (uint)_view.CorrectedFrame.Width, (uint)_view.CorrectedFrame.Height, null, true, true);//参考VeldridNSViewExample
            if (_backend == GraphicsBackend.Metal)
            {
                //_graphicsDevice = GraphicsDevice.CreateMetal(_options);
                //_swapChain = _graphicsDevice.ResourceFactory.CreateSwapchain(ref scd);
                _graphicsDevice = GraphicsDevice.CreateMetal(_options, scd);
                _swapChain = _graphicsDevice.MainSwapchain;
            }
            else if (_backend == GraphicsBackend.OpenGL)
            {
                throw new NotImplementedException("Current not support OpenGL on Mac");
            }
            else if (_backend == GraphicsBackend.Vulkan)
            {
                //need use MoltenVK
                _graphicsDevice = GraphicsDevice.CreateVulkan(_options, scd);
                _swapChain = _graphicsDevice.MainSwapchain;
                //throw new NotImplementedException("Current not support Vulkan on Mac");
            }
            _resources = new DisposeCollectorResourceFactory(_graphicsDevice.ResourceFactory);
            InvokeGraphicsDeviceCreated();

            Run();
        }

        private void OnViewSizeChanged()
        {
            if (_graphicsDevice != null)
            {
                //MTLSwapchain内部自动转换成Pixel
                _swapChain.Resize((uint)_view.CorrectedFrame.Width, (uint)_view.CorrectedFrame.Height);
                InvokeResized();
            }
        }
    }
}
