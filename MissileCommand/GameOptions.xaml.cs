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
using System.Windows.Shapes;

namespace FinalProject
{

    public class GameOptionsContainer
    {
        public int _noMissiles;
        public int _noCities;
        public bool _gameSpeedIncrease;
        public string _initials;

        public GameOptionsContainer(int noMissiles, int noCities, bool gameSpeedincrease)
        {
            _noMissiles = noMissiles;
            _noCities = noCities;
            _gameSpeedIncrease = gameSpeedincrease;
            _initials = "AAA";
        }
    }
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class GameOptions : Window
    {
        private GameOptionsContainer _container;
        private bool _isLoaded;
        public GameOptions(GameOptionsContainer container)
        {
            InitializeComponent();

            _container = container;


            // setup the missile slider
            DoubleCollection missileSliderTicks = new DoubleCollection();
            missileSliderTicks.Add(5);
            missileSliderTicks.Add(10);
            missileSliderTicks.Add(15);
            missileSliderTicks.Add(20);
            missileSliderTicks.Add(25);
            missileSliderTicks.Add(30);
            missileSliderTicks.Add(31);
            
            this.sliderMissiles.Minimum = 5;
            this.sliderMissiles.Maximum = 31;
            this.sliderMissiles.IsSnapToTickEnabled = true;
            this.sliderMissiles.Ticks = missileSliderTicks;

            this.sliderMissiles.ValueChanged += new RoutedPropertyChangedEventHandler<double>(MissileSliderChanged);
        

            // setup cities slider
            DoubleCollection citiesSliderTicks = new DoubleCollection();
            citiesSliderTicks.Add(1);
            citiesSliderTicks.Add(2);
            citiesSliderTicks.Add(3);
            citiesSliderTicks.Add(4);
            citiesSliderTicks.Add(5);
            citiesSliderTicks.Add(6);

            this.sliderCities.Minimum = 1;
            this.sliderCities.Maximum = 6;
            this.sliderCities.IsSnapToTickEnabled = true;
            this.sliderCities.Ticks = citiesSliderTicks;

            this.sliderCities.ValueChanged += new RoutedPropertyChangedEventHandler<double>(CitiesSliderChanged);

            _isLoaded = true;
       }

        private void GameSpeedRadioChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (this.radioIncreasingSpeed.IsChecked == true)
                _container._gameSpeedIncrease = true;
            else
                _container._gameSpeedIncrease = false;
        }
    
        public void MissileSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded)
                return;
            double controlValue = ((Slider)sender).Value;
            if (controlValue == 31)
                this.labelMissiles.Content = "unlimited";
            else
                this.labelMissiles.Content = (int)controlValue + "";

            _container._noMissiles = (int)controlValue;
        }

        public void CitiesSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded)
                return;
            double controlValue = ((Slider)sender).Value;
            this.labelCities.Content = (int)controlValue;    
            _container._noCities = (int)controlValue;
        }

        private void radioConstantSpeedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (this.radioIncreasingSpeed.IsChecked == true)
                _container._gameSpeedIncrease = true;
            else
                _container._gameSpeedIncrease = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            this.Close();
        }

      
        /*
         * This method filters the contents of TextBoxInitials so that it only contains capital letters of 
         * a maximum length of three characters
         */
        private void TextBoxInitials_KeyUp(object sender, KeyEventArgs e)
        {
            List<Key> dontBotherKeyList = new List<Key>();
            dontBotherKeyList.Add(Key.Back);
            dontBotherKeyList.Add(Key.Delete);
            dontBotherKeyList.Add(Key.Left);
            dontBotherKeyList.Add(Key.Right);
            dontBotherKeyList.Add(Key.Up);
            dontBotherKeyList.Add(Key.Down);
            dontBotherKeyList.Add(Key.LeftShift);
            dontBotherKeyList.Add(Key.RightShift);

            if (dontBotherKeyList.Contains(e.Key))
                return;

            String unFilteredInitials = this.TextBoxInitials.Text;
            String filteredInitials = "";
     
            for (int i =0;  i < unFilteredInitials.Length;   ++i)
                if (char.IsLetter(unFilteredInitials.ElementAt(i)))
                    filteredInitials += char.ToUpper(unFilteredInitials.ElementAt(i));
     
           if(filteredInitials.Length > 3)
                filteredInitials = filteredInitials.Substring(0, Math.Min(3,Math.Max(0, this.TextBoxInitials.Text.Length)));
            
            if(filteredInitials.CompareTo(this.TextBoxInitials.Text) != 0)
                this.TextBoxInitials.Text = filteredInitials;

            this.TextBoxInitials.CaretIndex = filteredInitials.Length;

            _container._initials = filteredInitials;
        }

        
        private void TextBoxInitials_GotFocus(object sender, RoutedEventArgs e)
        {
            this.TextBoxInitials.Text = "";
        }
    
        
    
    
    
    }
}
