﻿using Veldrid.Maui.Controls;
using Veldrid.Maui.Controls.Base;

namespace Veldrid.Maui.Samples
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            /*            if (OperatingSystem.IsWindows())
                        {
                            //veldridView.Drawable = new ComputeTextureApplication(null);//Success
                            //veldridView.Drawable = new OffscreenApplication(null);//Success
                            //veldridView.Drawable = new InstancingApplication(null);//Success
                            //veldridView.Drawable = new AnimatedMeshApplication(null);//Success
                            veldridView.Drawable = new ComputeParticlesApplication(null);//Fail
                            //veldridView.Drawable = new GettingStartedApplication(null);//Success
                        }
                        else if (OperatingSystem.IsAndroid())
                        {
            #if ANDROID
                            AssimpNet.Platform.Init();
            #endif
                            veldridView.Drawable = new ComputeTextureApplication(null);//Success
                            //veldridView.Drawable = new OffscreenApplication(null);//Success
                            //veldridView.Drawable = new InstancingApplication(null);//Success
                            //veldridView.Drawable = new AnimatedMeshApplication(null);//Fail
                            //veldridView.Drawable = new ComputeParticlesApplication(null);//Fail
                            //veldridView.Drawable = new GettingStartedApplication(null);//Success
                        }
                        else if (OperatingSystem.IsIOS())
                        {
                            //veldridView.Drawable = new ComputeTextureApplication(null);//Success
                            //veldridView.Drawable = new OffscreenApplication(null);
                            veldridView.Drawable = new InstancingApplication(null);
                            //veldridView.Drawable = new AnimatedMeshApplication(null);
                            //veldridView.Drawable = new ComputeParticlesApplication(null);
                            //veldridView.Drawable = new GettingStartedApplication(null);//Success
                        }
                        else if (OperatingSystem.IsMacCatalyst())
                        {
                            //Current not work for MacCatalyst, because need build SPIRV for Maccatalyst.
                            //veldridView.Drawable = new ComputeTextureApplication(null);
                            //veldridView.Drawable = new OffscreenApplication(null);
                            //veldridView.Drawable = new InstancingApplication(null);
                            //veldridView.Drawable = new AnimatedMeshApplication(null);
                            //veldridView.Drawable = new ComputeParticlesApplication(null);
                        }*/

#if ANDROID
                AssimpNet.Platform.Init();
#endif
            var buttonContainer = new HorizontalStackLayout()
            {
                Children =
                {
                    new Button(){ Text = nameof(Veldrid.Maui.Samples.Core.GettingStarted.GettingStartedDrawable)},
                    new Button(){ Text =nameof(Veldrid.Maui.Samples.Core.ComputeTexture.ComputeTextureApplication)},
                    new Button(){ Text = nameof(Veldrid.Maui.Samples.Core.AnimatedMesh.AnimatedMeshApplication)},
                    new Button(){ Text = nameof(Veldrid.Maui.Samples.Core.ComputeParticles.ComputeParticlesApplication)},
                    new Button(){ Text = nameof(Veldrid.Maui.Samples.Core.Instancing.InstancingApplication)},
                    new Button(){ Text = nameof(Veldrid.Maui.Samples.Core.Offscreen.OffscreenApplication)},
                    new Button(){ Text = nameof(Veldrid.Maui.Samples.Core.TexturedCube.TexturedCubeDrawable)},
                    new Button(){ Text = "Remove"},
                }
            };
            var platformView = new VeldridView() { };
            var camera = new SimpleCamera();
            PanGestureRecognizer panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += (sender, e) =>
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Running:
                        camera.UpdatePanPoint((float)e.TotalX, (float)e.TotalY);
                        break;
                }
            };
            platformView.GestureRecognizers.Add(panGesture);
            foreach (var view in buttonContainer.Children)
            {
                if (view is Button)
                {
                    var button = view as Button;
                    button.Clicked += (s, e) =>
                    {
                        if (button.Text == nameof(Veldrid.Maui.Samples.Core.GettingStarted.GettingStartedDrawable))
                            platformView.Drawable = new Veldrid.Maui.Samples.Core.GettingStarted.GettingStartedDrawable();
                        else if (button.Text == nameof(Veldrid.Maui.Samples.Core.ComputeTexture.ComputeTextureApplication))
                            platformView.Drawable = new Veldrid.Maui.Samples.Core.ComputeTexture.ComputeTextureApplication();
                        else if (button.Text == nameof(Veldrid.Maui.Samples.Core.AnimatedMesh.AnimatedMeshApplication))
                            platformView.Drawable = new Veldrid.Maui.Samples.Core.AnimatedMesh.AnimatedMeshApplication(camera);
                        else if (button.Text == nameof(Veldrid.Maui.Samples.Core.ComputeParticles.ComputeParticlesApplication))
                            platformView.Drawable = new Veldrid.Maui.Samples.Core.ComputeParticles.ComputeParticlesApplication();//bug
                        else if (button.Text == nameof(Veldrid.Maui.Samples.Core.Instancing.InstancingApplication))
                            platformView.Drawable = new Veldrid.Maui.Samples.Core.Instancing.InstancingApplication(camera);
                        else if (button.Text == nameof(Veldrid.Maui.Samples.Core.Offscreen.OffscreenApplication))
                            platformView.Drawable = new Veldrid.Maui.Samples.Core.Offscreen.OffscreenApplication(camera);
                        else if (button.Text == nameof(Veldrid.Maui.Samples.Core.TexturedCube.TexturedCubeDrawable))
                            platformView.Drawable = new Veldrid.Maui.Samples.Core.TexturedCube.TexturedCubeDrawable();
                        else if (button.Text == "Remove")
                            platformView.Drawable = null;
                    };
                }
            }

            scrollView.Content = (buttonContainer);
            veldridContainer.Children.Add(platformView);
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
        }
    }
}
