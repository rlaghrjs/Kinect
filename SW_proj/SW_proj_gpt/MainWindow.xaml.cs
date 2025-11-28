using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace SW_proj_gpt
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinectSensor;
        private DispatcherTimer gameTimer;
        private DispatcherTimer wordDropTimer;
        private Random random = new Random();
        private List<TextBlock> fallingWords = new List<TextBlock>();
        private int score = 0;
        private bool isPlayerDetected = false;
        private TextBlock currentWordBlock;
        private Dictionary<string, string[]> wordMeanings;
        private Joint leftHandJoint;
        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;

        public MainWindow()
        {
            InitializeComponent();
            InitializeKinect();
            InitializeWordMeanings();
            StartTimers();
        }

        private void InitializeWordMeanings()
        {
            wordMeanings = new Dictionary<string, string[]>
            {
                { "Apple", new[] { "사과", "바나나", "오렌지" } },
                { "Banana", new[] { "바나나", "사과", "포도" } },
                { "Cherry", new[] { "체리", "수박", "레몬" } },
                { "Date", new[] { "대추야자", "복숭아", "배" } },
                { "Grape", new[] { "포도", "딸기", "블루베리" } }
            };
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
                        isPlayerDetected = true;
                        leftHandJoint = trackedSkeleton.Joints[JointType.HandLeft];
                    }
                    else
                    {
                        isPlayerDetected = false;
                    }
                }
            }
        }

        private void StartTimers()
        {
            gameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            wordDropTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            wordDropTimer.Tick += (s, _) => { if (isPlayerDetected) CreateFallingWord(); };
            wordDropTimer.Start();
        }

        private void CreateFallingWord()
        {
            string[] words = wordMeanings.Keys.ToArray();
            string word = words[random.Next(words.Length)];

            TextBlock wordBlock = new TextBlock
            {
                Text = word,
                FontSize = 24,
                Foreground = Brushes.White
            };

            Canvas.SetLeft(wordBlock, random.Next((int)GameCanvas.ActualWidth - 100));
            Canvas.SetTop(wordBlock, 0);
            GameCanvas.Children.Add(wordBlock);
            fallingWords.Add(wordBlock);
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            for (int i = fallingWords.Count - 1; i >= 0; i--)
            {
                TextBlock word = fallingWords[i];
                double top = Canvas.GetTop(word);
                Canvas.SetTop(word, top + 5);

                if (top > GameCanvas.ActualHeight)
                {
                    GameCanvas.Children.Remove(word);
                    fallingWords.RemoveAt(i);
                }
                else
                {
                    TrackLeftHand();
                }
            }
        }

        private void TrackLeftHand()
        {
            foreach (var word in fallingWords)
            {
                Point wordPosition = new Point(Canvas.GetLeft(word), Canvas.GetTop(word));
                var handPosition = kinectSensor.CoordinateMapper.MapSkeletonPointToColorPoint(leftHandJoint.Position, ColorImageFormat.RgbResolution640x480Fps30);
                double handX = handPosition.X * GameCanvas.ActualWidth / kinectSensor.ColorStream.FrameWidth;
                double handY = handPosition.Y * GameCanvas.ActualHeight / kinectSensor.ColorStream.FrameHeight;

                if (Math.Abs(handX - wordPosition.X) < 50 && Math.Abs(handY - wordPosition.Y) < 50)
                {
                    currentWordBlock = word;
                    GameCanvas.Children.Remove(word);
                    fallingWords.Remove(word);
                    PauseGame();
                    ShowWordOptions(word.Text);
                    break;
                }
            }
        }

        private void ShowWordOptions(string word)
        {
            QuestionText.Text = $"What is the meaning of '{word}'?";
            var options = wordMeanings[word].OrderBy(x => random.Next()).ToArray();
            Option1.Content = options[0];
            Option2.Content = options[1];
            Option3.Content = options[2];
            AnswerPanel.Visibility = Visibility.Visible;
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton.Content.ToString() == wordMeanings[currentWordBlock.Text][0])
            {
                score += 10;
                ScoreText.Text = $"Score: {score}";
                MessageBox.Show("Correct!");
            }
            else
            {
                MessageBox.Show("Wrong answer!");
            }
            AnswerPanel.Visibility = Visibility.Collapsed;
            ResumeGame();
        }

        private void PauseGame()
        {
            gameTimer.Stop();
            wordDropTimer.Stop();
        }

        private void ResumeGame()
        {
            gameTimer.Start();
            wordDropTimer.Start();
        }
    }
}
