/**
 * Michael Peterson
 * 
 * CSCD 371 
 * Final Project
 * 
 * 6/9/2015
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Timers;
using System.Media;
using System.IO;
namespace FinalProject
{
/* ---------------------------------------------------------------*
 *                         MainWindow                             *
 * ---------------------------------------------------------------*/
    public partial class MainWindow : Window
    {
        private Brush _transparentBrush;
        private Brush _blueBrush;
        private Brush _redBrush;
        private bool _infiniteMissles;
        private List<Missile> _enemyMissiles;
        private List<GroundAsset> _groundAssets;
        private int _noCities;
        private bool _increaseGameSpeed;
     
        private int _enemyMissileBlastRadius;
        private int _playerMissileSpeed;
        private int _playerMissileBlastRadius;
        private int _playerMissileCount;
        
        private Timer _timer;
        private Random _random;
        private List<GameLevel> _gameLevels;
        private int _currentLevel;
        private GameLevel _currentLevelData;
        private GameState _state;
        private int _currentScore;
        private double _totalPausedTime;

        private double _lastPauseStartTime;
        private double _lastPauseEndTime;

        private List<HighScore> _highScores;

        private int _highestScore;
        private string _initials;


        private enum GameState { titleScreen = 0, paused = 1, inPlay = 2, gameOver=3, passedLevel=4, setup=5, tallyCurrentScore=6, tallyFinalScore=7}
        
        public MainWindow()
        {

            _lastPauseEndTime = -1;
            _lastPauseStartTime = -1;

            InitializeComponent();
            _random = new Random(DateTime.Now.Millisecond % 10);
            _transparentBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            _blueBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
            _redBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            
            _highScores = new List<HighScore>();


            _playerMissileSpeed = 1000;
            _playerMissileBlastRadius = 100;
            _enemyMissileBlastRadius = 50;
            _currentLevel = 1;
            _currentScore = 0;

            SetupNewGame();
            
            // register mouse event handlers
            this.MouseMove += new MouseEventHandler(MouseMoved);
            this.MouseUp += new MouseButtonEventHandler(MouseClicked);

            TranslateTransform crossHairPosition = new TranslateTransform(0.0, 0.0);
            this.Crosshair.RenderTransform = crossHairPosition;
            this.Crosshair.Fill = _transparentBrush;

            // setup the timer
            _timer = new Timer(1);
            _timer.Start();
            _timer.Elapsed += new ElapsedEventHandler(timerHandler);
            _timer.Enabled = true;
            _timer.Start();

            ReadHighScores();
           
            _state = GameState.inPlay;
        }
    
        public void ReadHighScores()
        {
            string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = currentPath + "\\highscores.txt";
            if (System.IO.File.Exists(filePath))
            {
                string[] lines = System.IO.File.ReadAllLines(filePath);

                if (lines.Count() == 0)
                    _highestScore = 0;

                foreach (string thisLine in lines)
                {
                    string initials = thisLine.Split(new char[] { ' ' })[0];
                    int score = Convert.ToInt32(thisLine.Split(new char[] { ' ' })[1]);
                    _highScores.Add(new HighScore(initials, score));
                }

                _highScores.Sort();
                _highestScore = _highScores[_highScores.Count - 1].getScore();
                this.highScore.Text = _highestScore.ToString();
            }
        }

        public void WritehighScores()
        {
            if (_highScores.Count == 0)
                return;

            string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = currentPath + "\\highscores.txt";

            StreamWriter writer = File.CreateText(filePath);

            foreach (HighScore thisEntry in _highScores)
            {
                string thisLine = thisEntry.getInitials() + " " + thisEntry.getScore();
                writer.WriteLine(thisLine);
                Console.WriteLine(thisLine);
            }
            writer.Close();
        }

        public void togglePuased()
        {
            if (_state == GameState.paused)
            {
                _lastPauseEndTime = (DateTime.Now - DateTime.MinValue).TotalMilliseconds;
                if(_lastPauseStartTime != -1)
                    _totalPausedTime += _lastPauseEndTime - _lastPauseStartTime;
                TransitionToInPlay();
            }

            else if (_state == GameState.inPlay)
            {
                _lastPauseStartTime = (DateTime.Now - DateTime.MinValue).TotalMilliseconds;
                TransitionToPaused();
            }
        }

        public double getAdjustedTimeInMillis()
        {
            return (DateTime.Now - DateTime.MinValue).TotalMilliseconds - _totalPausedTime;
        }

        public void doRestart()
        {
            CleanupEnemyMissiles();
            CleanupGroundAssets();
            SetupNewGame();
        }

        public void CleanupEnemyMissiles()
        {
            if (_enemyMissiles == null)
                return;
            foreach (Missile thisMissile in _enemyMissiles)
                thisMissile.Cleanup();

            _enemyMissiles = null;
        }

        public void CleanupGroundAssets()
        {
            foreach (GroundAsset thisAsset in _groundAssets)
            {
                thisAsset.cleanUp();
                thisAsset.Revive();
            }
        }
        public void SetupNewGame()
        {
            _currentScore = 0;
            _state = GameState.setup;
            SetupGameLevels();
            _currentLevelData = _gameLevels[0];
            this.currentLevel.Text = "Level 1";
            GameOptionsContainer gameOptionsContainer = new GameOptionsContainer(31, 6, true);
            GameOptions gameOptions = new GameOptions(gameOptionsContainer);
            gameOptions.ShowDialog();

            _initials = gameOptionsContainer._initials;
            this.playerInitialsText.Text = _initials;

            _playerMissileCount = gameOptionsContainer._noMissiles;
            _infiniteMissles = (_playerMissileCount == 31);
            if (_playerMissileCount == 31) _playerMissileCount = 30;
            _noCities = gameOptionsContainer._noCities;
            _increaseGameSpeed = gameOptionsContainer._gameSpeedIncrease;

            SetupEnemyMissiles();
            SetupGroundAssets();
            _state = GameState.inPlay;
            this.statusText.Text = "";
            this.highScoresText.Text = "";
        }

        
        public void TransitionToInPlay()
        {
            if (_state == GameState.paused)
                _state = GameState.inPlay;
            this.statusText.Text = "";
        }

        public void SetupGameLevels()
        {
            if (_gameLevels != null)
                return;
            _gameLevels = new List<GameLevel>();
            _gameLevels.Add(new GameLevel(13,  35, 1,1));
            _gameLevels.Add(new GameLevel(15,  60, 1,2));
            _gameLevels.Add(new GameLevel(18,  90, 1,3));
            _gameLevels.Add(new GameLevel(24, 102,1,4));
            _gameLevels.Add(new GameLevel(27, 130,1,5));
            _gameLevels.Add(new GameLevel(30, 180,1,6));
            _gameLevels.Add(new GameLevel(35, 210,1,7));
            _gameLevels.Add(new GameLevel(40, 230,1,8));
            _gameLevels.Add(new GameLevel(45, 260,1,9));
            _gameLevels.Add(new GameLevel(50, 290,1,10));
        }

        public void timerHandler(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() => ThreadSafeTimerTickHandler()), new object[] { });
        }

        public void attackGround()
        {
            if(_groundAssets.Count > 0)
            {
                int thisMissileSpeed = (_increaseGameSpeed) ? _currentLevelData.getEnemyMissileSpeed() : _gameLevels[0].getEnemyMissileSpeed();
                List<GroundAsset> standingAssets = new List<GroundAsset>();
                foreach (GroundAsset thisAsset in _groundAssets)
                    if (!thisAsset.Distroyed())
                        standingAssets.Add(thisAsset);

                if (standingAssets.Count == 0)
                    return;
                int index = (_random.Next(standingAssets.Count) );
                GroundAsset target = standingAssets[index];

                Point destination = GetCenterOfShape(target.Target());
                Point launchLocation = new Point(_random.Next(1024), 0);
                
                Missile thisMissile = null;
                for (int i = 0; i < _enemyMissiles.Count; ++i)
                    if ((thisMissile = _enemyMissiles[i]).IsReady())
                        break;
                if(thisMissile != null)
                    thisMissile.beginLaunch(launchLocation, destination, thisMissileSpeed);
            }
        }
        
        public void ThreadSafeTimerTickHandler()
        {
            if (_state == GameState.paused)
                return;
            if(_state == GameState.setup)
            { return; }

            if (_state == GameState.inPlay)
            {
                double currentTimeInMillis = getAdjustedTimeInMillis();

                int currentSecond = (int)((currentTimeInMillis / 1000.0) % 10);

                if(_random.Next(101) <= _currentLevelData.getLaunchOdds())
                    attackGround();


                // update each of the players missiles, and check for collisions
                foreach (GroundAsset thisAsset in _groundAssets)
                {
                    if (thisAsset.IsSilo())
                    {
                        Missile playerMissile = thisAsset.GetMissile();
                        playerMissile.timerCLick(currentTimeInMillis);

                        if (playerMissile.IsExploding())
                        {
                            foreach (Missile enemyMissile in _enemyMissiles)
                            {
                                if (enemyMissile.IsLaunching())
                                {
                                    if (AreMissilesIntersecting(playerMissile, enemyMissile))
                                        enemyMissile.detinate();
                                }
                            }// end foreach
                        }// end if(playerMissile.IsExploding())
                    }// end if(thisAsset.IsSolo())

                }// end foreach

                // update the enemy missiles and check for collisions
                foreach (Missile thisMissile in _enemyMissiles)
                {
                    thisMissile.timerCLick(currentTimeInMillis);

                    if (thisMissile.IsExploding())
                    {
                        foreach (GroundAsset thisAsset in _groundAssets)
                        {
                            if (IsMissileHittingAsset(thisMissile, thisAsset))
                                thisAsset.Distroy();
                        }// end foreach
                    }// end if(thisMissile.IsExploding)
                }// end foreach
        
                // check to see if all of the cities are gone (if so then it is game over)
                if (OutOfCities())
                {
                    TransitionToGameOver();
                    return;
                }
                // check to see if the enemy is out of missiles, if so then it is on the the next level
                if (OutOfEnemyMissiles())
                {
                    TransitionToNextLevel();
                    return;
                }
            
            }
        
        }

        public void TransitionToPaused()
        {
            if (_state == GameState.gameOver || _state == GameState.paused)
                return;
            _state = GameState.paused;

            this.statusText.Text = "Paused";
            this.statusText.FontSize = 50;
            this.statusText.Foreground = this._blueBrush;
        }

        public void TransitionToGameOver()
        {
            _state = GameState.setup;
            this.statusText.Text = "GAME OVER";
            this.statusText.FontSize = 50;
            this.statusText.Foreground = _redBrush;


            // add the current player/score to the high scores list
            _highScores.Add(new HighScore(_initials, _currentScore));

            this.highScoresText.Foreground = _blueBrush;
            this.highScoresText.FontSize = 50;

            this.highScoresText.Text = "High Scores\n";
            _highScores.Sort();
            
            for(int i = _highScores.Count -1; i > _highScores.Count -5; --i)
                this.highScoresText.Text += _highScores[i] + Environment.NewLine;

            foreach (GroundAsset thisAsset in _groundAssets)
                thisAsset.cleanUp();

            CleanupEnemyMissiles();

            _state = GameState.gameOver;
            WritehighScores();
        }

        public void TransitionToNextLevel()
        {
            _state = GameState.setup;
            CalculateCurrentScore();
            _currentLevelData = _gameLevels[_currentLevel++];
            this.currentLevel.Text = "Level " + _currentLevel;
            SetupEnemyMissiles();
            ResetPlayerMissiles();

            if (_currentScore> _highestScore)
                _highestScore = _currentScore;

            this.highScore.Text = _highestScore.ToString();

            _state = GameState.inPlay;
        }

        public void CalculateCurrentScore()
        {
            int noRemainingSilos = 0;
            int noRemainingCities = 0;
            // total the remaining silos
            foreach(GroundAsset thisAsset in _groundAssets)
            {
                if (thisAsset.IsSilo() && !thisAsset.Distroyed())
                    ++noRemainingSilos;

                if (!thisAsset.IsSilo() && !thisAsset.Distroyed())
                    ++noRemainingCities;
            }
            
            _currentScore += (5 * noRemainingSilos) + (100 * noRemainingCities);
            this.currentScore.Text = _currentScore + "";
        }

        public bool AreMissilesIntersecting(Missile explodingMissle, Missile launchingMissile)
        {
            return Math.Sqrt(Math.Pow(explodingMissle.Location().X - launchingMissile.Location().X, 2) + Math.Pow(explodingMissle.Location().Y - launchingMissile.Location().Y, 2)) <= explodingMissle.GetExplosion().Width / 2;
        }

        public bool IsMissileHittingAsset(Missile enemyMissile, GroundAsset asset)
        {
            return Math.Sqrt(Math.Pow(enemyMissile.Location().X - asset.Location().X, 2) + Math.Pow(enemyMissile.Location().Y - asset.Location().Y, 2)) <= enemyMissile.GetExplosion().Width / 2;
        }
        
        public void SetupGroundAssets()
        {
            _groundAssets = new List<GroundAsset>();
            GroundAsset thisAsset = null;
            for (int i = 0; i < 30; ++i)
            {
                Shape thisSilo = (Shape)this.FindName(ConvertMissileNoToBaseShapeName(i));
                Missile thisMissile = new Missile(_blueBrush,this.LayoutRoot,true, _playerMissileBlastRadius, this);
                thisAsset = new GroundAsset(thisSilo, this.LayoutRoot, _groundAssets,true, thisMissile);

                _groundAssets.Add(thisAsset);
            }
            for (int i = 0; i < 6; ++i)
            {
                _groundAssets.Add(new GroundAsset((Shape)this.FindName("City" + i), this.LayoutRoot, _groundAssets,false));
            }
        
            // disable the appropriate number of cities and missiles;
            int noRemainingCitiesToDisable = 6 - _noCities;
            int thisIndex = 0;
            while(noRemainingCitiesToDisable > 0)
            {
                thisIndex = _random.Next(36);
                thisAsset = _groundAssets[thisIndex];
                if(!thisAsset.Distroyed() && !thisAsset.IsSilo())
                {
                    thisAsset.Distroy();
                    --noRemainingCitiesToDisable;
                }
            }
            
            int noRemainingSilosToDisable = 30 - _playerMissileCount;
            while(noRemainingSilosToDisable > 0)
            {
                thisIndex = _random.Next(36);
                thisAsset = _groundAssets[thisIndex];
                if(!thisAsset.Distroyed() && thisAsset.IsSilo())
                {
                    thisAsset.Distroy();
                    --noRemainingSilosToDisable;
                }
            }
        }

        public bool OutOfEnemyMissiles()
        {
            foreach (Missile thisMissile in _enemyMissiles)
                if (!thisMissile.IsDead() )
                    return false;
            return true;
        }
    
        public bool OutOfCities()
        {
            foreach(GroundAsset thisAsset in _groundAssets)
                if(!thisAsset.IsSilo() && !thisAsset.Distroyed())
                    return false;
            return true;
        }
        public void SetupEnemyMissiles()
        {
            _enemyMissiles = new List<Missile>();
            for (int i = 0; i < _currentLevelData.getNoEnemyMissiles(); ++i )
            {
                _enemyMissiles.Add(new Missile(_redBrush, this.LayoutRoot, false, _enemyMissileBlastRadius, this));
            }
        }

        public void ResetPlayerMissiles()
        {
            int noRemainingMissilesToDisable = 30 - _playerMissileCount;
            foreach(GroundAsset thisAsset in _groundAssets)
                if (thisAsset.IsSilo())
                    thisAsset.Revive();
            
            GroundAsset asset = null;
            while(noRemainingMissilesToDisable > 0)
            {
                int thisIndex = _random.Next(36);
                asset = _groundAssets[thisIndex];
                if(asset.IsSilo())
                {
                    asset.Distroy();
                    --noRemainingMissilesToDisable;
                }
            }
        }

        public Point GetCenterOfShape(Shape shape)
        {
            return new Point(Canvas.GetLeft(shape) + shape.ActualWidth / 2, Canvas.GetTop(shape) + shape.ActualHeight / 2);
        }

        public void MouseMoved(object sender, MouseEventArgs e)
        {
            this.Crosshair.RenderTransform = new TranslateTransform(e.GetPosition(this.LayoutRoot).X, e.GetPosition(this.LayoutRoot).Y);
        }
    
        public void MouseClicked(object sender, MouseEventArgs e)
        {
            if (_state == GameState.inPlay)
            {
                double x = e.GetPosition(this.LayoutRoot).X;
                double y = e.GetPosition(this.LayoutRoot).Y;
                launchPlayerMissle(x, y);
            }
        }
    
        public void launchPlayerMissle(double x, double y)
        {
            // check to see if we have any missles left
            List<GroundAsset> usableMissiles = new List<GroundAsset>();
            GroundAsset chosenAsset = null;
            foreach (GroundAsset thisAsset in _groundAssets)
                if (!thisAsset.Distroyed() && thisAsset.IsSilo() && thisAsset.GetMissile().IsReady())
                    usableMissiles.Add(thisAsset);
            
            if (usableMissiles.Count == 0)
                return;


            chosenAsset = usableMissiles[_random.Next(usableMissiles.Count)];

            Point destination = new Point(x,y);
            Point launchLocation = GetCenterOfShape(chosenAsset.Target());
            chosenAsset.GetMissile().beginLaunch(launchLocation, destination, _playerMissileSpeed);

            if (!_infiniteMissles)
                chosenAsset.Distroy();
            
        }

        public enum bases{Alpha=0,Delta = 1,Omega = 2,}
        
        public string ConvertMissileNoToBaseShapeName(int missileNo)
        {
            return getBaseStringFromInt(missileNo / 10) + missileNo % 10;
        }

        public string getBaseStringFromInt(int baseNo)
        {
            switch(baseNo)
            {
                case (int)bases.Alpha:
                    return "Alpha";
                case (int)bases.Delta:
                    return "Delta";
                case (int)bases.Omega:
                    return "Omega";
            }
            return "";
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.P)
                togglePuased();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer.Enabled = false;
            _timer.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            togglePuased();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            doRestart();
        }
    
    }

    /* ---------------------------------------------------------------*
     *                         GameLevel                              *
     * ---------------------------------------------------------------*/
    public class GameLevel
    {
        private int _noEnemyMissiles;
        private int _enemyMissileSpeed;
        private int _launchOdds;
        private int _levelNo;

        public GameLevel(int noEnemyMissiles, int enemyMissileSpeed, int launchOdds, int levelNo)
        {
            _noEnemyMissiles = noEnemyMissiles;
            _enemyMissileSpeed = enemyMissileSpeed;
            _launchOdds = launchOdds;
            _levelNo = levelNo;
        }

        public int getNoEnemyMissiles() { return _noEnemyMissiles; }
        public int getEnemyMissileSpeed() { return _enemyMissileSpeed; }
        public int getLaunchOdds() { return _launchOdds; }
        public int getLevelNo() { return _levelNo; }
     }

    /* ---------------------------------------------------------------*
     *                         Missile                                *
     * ---------------------------------------------------------------*/
    public class Missile
    {

        private Point _destination;
        private Point _currentLocation;
        private Point _launchpadLocation;
        private MissileState _state;
        private Brush   _tailColor;
        private Line    _tail;
        private Ellipse _explosion;
        private double _speed;
        private double _launchTime;
        private double _explodeStartTime;
        private double _blastRadius;
        private double _explosionDuration;
        private Canvas _canvas;
        private double _launchAngle;
        private double _expectedArrivalTime;
        private int _frameNo;
        private static Brush white, red, yellow, orange;
        private bool _relaunchable;
        private MediaPlayer _launchSoundPlayer;
        private MediaPlayer _explosionSoundPlayer;
        private MainWindow _caller;

        // Note: all times are in milliseconds and all coordinates are in pixels;
        //       _speed is measured pixels per second
        public enum MissileState{ready=0,dead=1,launching=2,exploding=3}

        public bool IsReady() { return _state == MissileState.ready; }
        public bool IsExploding() { return _state == MissileState.exploding; }
        public bool IsLaunching() { return _state == MissileState.launching; }
        public bool IsDead() { return _state == MissileState.dead; }

        public void detinate() 
        {
             _explodeStartTime = _caller.getAdjustedTimeInMillis();
             _state = MissileState.exploding;
             _canvas.Children.Remove(_tail);

             _launchSoundPlayer.Stop();
             
             _explosionSoundPlayer.Play();
        }

        public void beginLaunch(Point launchpadLocation, Point destination, double speed)
        {
            if (_state == MissileState.ready)
            {
                _launchSoundPlayer.Play();   

                _state = MissileState.launching;
                _tail = new Line();
                _tail.Stroke = _tailColor;
                _canvas.Children.Add(_tail);
                _destination = destination;
                _launchTime = _caller.getAdjustedTimeInMillis();
                _launchpadLocation = launchpadLocation;
                _speed = speed;

                double distanceToTarget = CalcDistance(_destination, _launchpadLocation);
                double expectedTravelTimeElapsed = distanceToTarget / (_speed / 1000);
                _expectedArrivalTime = _launchTime + expectedTravelTimeElapsed; 
                
                
                if(_launchpadLocation.X >= _destination.X)
                    _launchAngle = Math.Atan((_launchpadLocation.Y - _destination.Y)/(_launchpadLocation.X - _destination.X));
                else
                    _launchAngle = Math.Atan((_launchpadLocation.Y - _destination.Y) / (_destination.X - _launchpadLocation.X));

                _explosion = new Ellipse();
                _explosion.Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                _explosion.RenderTransform = new TranslateTransform(_destination.X, _destination.Y);
                _explosion.Width = 5;
                _explosion.Height = 5;
                _canvas.Children.Add(_explosion);
            }
        }

        public void timerCLick(double currentTimeInMillis)
        {
            if(_state != MissileState.dead)
                ++_frameNo;
        
            double currentTimeInSeconds = currentTimeInMillis / 1000;
            
            if(_state ==  MissileState.launching)
            {
                double launchTimeInSeconds = _launchTime / 1000;
                double distance = (currentTimeInSeconds - launchTimeInSeconds) * _speed;
                double currentY = _launchpadLocation.Y - distance * Math.Sin(_launchAngle);
                
                double mulp = ( _launchpadLocation.X < _destination.X)? 1 : -1;
                double currentX = _launchpadLocation.X +  distance * mulp * Math.Cos(_launchAngle);
                
                _currentLocation = new Point( currentX, currentY);
                
                _tail.X2 = _launchpadLocation.X;
                _tail.X1 = _currentLocation.X;
                _tail.Y2 = _launchpadLocation.Y;
                _tail.Y1 = _currentLocation.Y;

                _explosion.RenderTransform = new TranslateTransform(_currentLocation.X - _explosion.Width / 2, _currentLocation.Y - _explosion.Height / 2);
           

                // determine if the missile has reached its destination
                if (currentTimeInMillis >= _expectedArrivalTime || CalcDistance(_currentLocation, _destination) < 10)
                    detinate();
                
            }
            if(_state == MissileState.exploding)
            {
                double millisSinceExplosion = currentTimeInMillis - _explodeStartTime;
                if(millisSinceExplosion > _explosionDuration * 1000)
                {
                    _explosionSoundPlayer.Stop();
                    _state = (_relaunchable) ? MissileState.ready : MissileState.dead;
                    _canvas.Children.Remove(_tail);
                    _canvas.Children.Remove(_explosion);

                    return;
                }

                _explosion.Width = Math.Abs(_blastRadius * Math.Sin(Math.PI * ( millisSinceExplosion / (_explosionDuration * 1000))));
                _explosion.Height = _explosion.Width;
                _explosion.RenderTransform = new TranslateTransform(_currentLocation.X - _explosion.Width / 2, _currentLocation.Y - _explosion.Height / 2);
            }
        
            if(_state == MissileState.launching || _state == MissileState.exploding)
            {
                Brush currentExplosionColor = null;
                int frameNoMod = _frameNo % 10;
                // white
                if (frameNoMod == 0 || frameNoMod == 4 || frameNoMod == 8)
                    currentExplosionColor = white;
                // red
                if (frameNoMod == 1 || frameNoMod == 5 || frameNoMod == 9)
                    currentExplosionColor = red;
                // yellow
                if (frameNoMod == 2 || frameNoMod == 6)
                    currentExplosionColor = yellow;
                // orange
                if (frameNoMod == 3 || frameNoMod == 7)
                    currentExplosionColor = orange;

                _explosion.Fill = currentExplosionColor;
            }
        
        }
        public Ellipse GetExplosion() { return _explosion; }
        public double CalcDistance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }

        public Point Location() { return _currentLocation; }

        public Missile(Brush tailColor, Canvas canvas, bool realaunchable, double blastRadius, MainWindow caller)
        {
            _caller = caller;
            _destination = new Point(0, 0);
            _currentLocation = new Point(0,0);
            _launchpadLocation = new Point(0, 0);
            _state = MissileState.ready;
            _tailColor = tailColor;
            _speed = 0;
            _launchTime = 0.0;
            _explodeStartTime = 0.0;
            _blastRadius = blastRadius;
            _explosionDuration = 2;
            _canvas = canvas;
            _frameNo = 0;
            _relaunchable = realaunchable;

            String currentPath = Environment.CurrentDirectory;
            
            _launchSoundPlayer = new MediaPlayer();
            _explosionSoundPlayer = new MediaPlayer();

            _launchSoundPlayer.Open(new Uri(@currentPath + "\\sounds\\missile.wav"));
            _explosionSoundPlayer.Open(new Uri(@currentPath + "\\sounds\\boom.wav")); ;

            white  = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            red    = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            yellow = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
            orange = new SolidColorBrush(Color.FromArgb(255, 255, 127, 0));
        }

        public void Cleanup()
        {
            if (_explosion != null)
            {
                _canvas.Children.Remove(_explosion);
            }
            if (_tail != null)
                _canvas.Children.Remove(_tail);
            _state = MissileState.ready;
            
        }
    }

/* ---------------------------------------------------------------*
 *                         GroundAsset                            *
 * ---------------------------------------------------------------*/
         public class GroundAsset
        {
            private bool _distroyed;
            private Shape _target;
            private Canvas _canvas;
            private List<GroundAsset> _container;
            private AssetType _assetType;
            private static Brush transparentBrush;
            private static Brush blueBrush;
            private Missile _missile;
            private Point _location;
            public enum AssetType{missileSilo=0,city=1}

            public GroundAsset(Shape target, Canvas canvas, List<GroundAsset> container, bool isSilo, Missile missile) : this(target,canvas,container,isSilo)
            {
                _missile = missile;
            }
            public GroundAsset(Shape target, Canvas canvas, List<GroundAsset> container, bool isSilo)
            {
                _distroyed = false;
                _target = target;
                _canvas = canvas;
                _container = container;
                if(isSilo)
                    _assetType = AssetType.missileSilo;
                else
                    _assetType = AssetType.city;
                transparentBrush = new SolidColorBrush(Color.FromArgb(0,0,0,0));
                blueBrush = new SolidColorBrush(Color.FromArgb(255,0,0,255));
                _location = new Point(Canvas.GetLeft(target) + target.ActualWidth / 2, Canvas.GetTop(target) + target.ActualHeight / 2);
            }

            public Point Location() { return _location; }

            public void setMissile(Missile missile)
            {
                _missile = missile;
            }
        
            public Missile GetMissile(){return _missile;}

            public void Distroy()
            {
                _distroyed = true;
                _target.Stroke = transparentBrush;
                _target.Fill   = transparentBrush;
            }
        
             public bool IsSilo(){return _assetType == AssetType.missileSilo;}

            public bool Distroyed(){return _distroyed;}
            public Shape Target() { return _target; }
        
            public void cleanUp()
            {
                if (_assetType == AssetType.missileSilo)
                    _missile.Cleanup();
            }

            public void Revive()
            {
                _distroyed = false;
                if(_target != null)
                {
                    _target.Stroke = blueBrush;
                    _target.Fill = blueBrush;
                }
            }
     }


/* ---------------------------------------------------------------*
 *                         HighScore                              *
 * ---------------------------------------------------------------*/
    public class HighScore : IComparable<HighScore>
    {
        private string _initials;
        private int _score;

        public HighScore(string initials, int score)
        {
            _initials = initials;
            _score = score;
        }

        public string getInitials() { return _initials; }
        public int getScore() { return _score; }

        int IComparable<HighScore>.CompareTo(HighScore other)
        {
            return _score - other._score;
        }

        public override string ToString()
        {
            return _initials + " " + _score;
        }
    }// end HighScore
}
