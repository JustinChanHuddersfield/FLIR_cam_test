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
using System.IO;
//using SpinnakerNET;
//using SpinnakerNET.GenApi;

namespace FLIRcamTest
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {   
            // Create camera objects
            private readonly FLIR cam = new FLIR();
        }

        private void FLIRConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FLIR.Connect();
                if (FLIR.IsConnected)
                {
                    FLIRConnectButton.Content = "Connected";
                    FLIRConnectButton.IsEnabled = false;
                    bitDepthComboBoxAxis.IsEnabled = false;
                    exposureButton.IsEnabled = false;
                    maxValCheckBox.IsEnabled = false;
                    
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void exposureButton_Click(object sender, RoutedEventArgs e)
        {
            FLIR.SetExposure(double.Parse(exposureTextBox.Text));
        }

        private void maxValCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FLIR.Text = "The CheckBox is checked.";
            FLIR.maxVal(double.Parse(maxValTextBox.Text));
        }

        private void maxValCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FLIR.Text = "The CheckBox is unchecked.";
            FLIR.maxVal(double.Parse(maxValTextBox.Text));
        }

        private void HandleThirdState(object sender, RoutedEventArgs e)
        {
            FLIR.Text = "The CheckBox is in the indeterminate state.";
            FLIR.maxVal(double.Parse(maxValTextBox.Text));
        }

    }
}
