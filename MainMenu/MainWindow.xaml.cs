using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using System.Windows.Media.Imaging;

namespace MainMenu
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinectSensor;
        private Skeleton[] skeletons;
        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;
        private Joint leftHandJoint;

        private const double MenuWidth = 0.5;  // 화면 상에서 메뉴 항목이 차지할 가로 범위
        private const double MenuHeight = 0.2; // 화면 상에서 메뉴 항목이 차지할 세로 범위
        private int currentMenuIndex = 0;

        // 메뉴 항목
        private string[] menuItems = { "Start Game", "Options", "Exit" };

        public MainWindow()
        {
            InitializeComponent();
            InitializeKinect();
        }

        private void InitializeKinect()
        {
            kinectSensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
            if (kinectSensor != null)
            {
                kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                kinectSensor.SkeletonStream.Enable();
                colorPixels = new byte[kinectSensor.ColorStream.FramePixelDataLength];
                colorBitmap = new WriteableBitmap(kinectSensor.ColorStream.FrameWidth, kinectSensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                KinectVideo.Source = colorBitmap;

                kinectSensor.AllFramesReady += Kinect_AllFramesReady;
                kinectSensor.Start();
            }
        }


        private void Kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (var colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyPixelDataTo(colorPixels);
                    colorBitmap.WritePixels(new Int32Rect(0, 0, colorFrame.Width, colorFrame.Height), colorPixels, colorFrame.Width * 4, 0);
                }
            }

            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                    var trackedSkeleton = skeletons.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
                    if (trackedSkeleton != null)
                    {
                        leftHandJoint = trackedSkeleton.Joints[JointType.HandLeft];

                        HandleMenuNavigation(leftHandJoint);
                    }
                }
            }
        }

        private void HandleMenuNavigation(Joint leftHandJoint)
        {
            var handX = leftHandJoint.Position.X;
            var handY = leftHandJoint.Position.Y;

            // 각 버튼의 중앙 좌표를 계산
            var startGameButtonCenter = GetButtonCenter(StartGameButton);
            var optionsButtonCenter = GetButtonCenter(OptionsButton);
            var exitButtonCenter = GetButtonCenter(ExitButton);

            // 각 버튼에 대해 손의 좌표와 비교
            double startGameButtonDistance = CalculateDistance(handX, handY, startGameButtonCenter.X, startGameButtonCenter.Y);
            double optionsButtonDistance = CalculateDistance(handX, handY, optionsButtonCenter.X, optionsButtonCenter.Y);
            double exitButtonDistance = CalculateDistance(handX, handY, exitButtonCenter.X, exitButtonCenter.Y);

            // 버튼 클릭 시 하이라이트 처리 및 클릭 이벤트 호출
            if (startGameButtonDistance < 0.2)
            {
                StartGameButton.Background = Brushes.Yellow;
                if (startGameButtonDistance < 0.05)  // 클릭 거리 기준 추가
                {
                    StartGameButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));  // 클릭 이벤트 호출
                }
            }
            else
            {
                StartGameButton.Background = Brushes.Transparent;
            }

            if (optionsButtonDistance < 0.2)
            {
                OptionsButton.Background = Brushes.Yellow;
                if (optionsButtonDistance < 0.05)  // 클릭 거리 기준 추가
                {
                    OptionsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));  // 클릭 이벤트 호출
                }
            }
            else
            {
                OptionsButton.Background = Brushes.Transparent;
            }

            if (exitButtonDistance < 0.2)
            {
                ExitButton.Background = Brushes.Yellow;
                if (exitButtonDistance < 0.05)  // 클릭 거리 기준 추가
                {
                    ExitButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));  // 클릭 이벤트 호출
                }
            }
            else
            {
                ExitButton.Background = Brushes.Transparent;
            }
        }

        private Point GetButtonCenter(Button button)
        {
            // 버튼의 좌표와 크기를 이용하여 중앙 좌표 계산
            var buttonX = button.TransformToAncestor(this).Transform(new Point(0, 0)).X + button.ActualWidth / 2;
            var buttonY = button.TransformToAncestor(this).Transform(new Point(0, 0)).Y + button.ActualHeight / 2;
            return new Point(buttonX, buttonY);
        }

        private double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            // 두 점 사이의 거리 계산
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }


        // 메뉴 항목 선택 (예: Start Game 클릭)
        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Start Game Selected");
            // 게임 시작 로직 추가
        }

        // 메뉴 항목 선택 (예: Options 클릭)
        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Options Selected");
            // 옵션 화면 로직 추가
        }

        // 메뉴 항목 선택 (예: Exit 클릭)
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Exit Selected");
            // 게임 종료 로직 추가
            Application.Current.Shutdown();
        }
    }
}
