using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ElevatorRaspPi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IHubProxy chat;
        public SynchronizationContext Context { get; set; }

        GpioController gpio;
        GpioPin pin6, pin5;
        GpioPinValue state = GpioPinValue.High;
        bool chamado, observer;
        DispatcherTimer timer;

        public MainPage()
        {
            this.InitializeComponent();

            chamado = false;
            observer = false;

            gpio = GpioController.GetDefault();
            pin6 = gpio.OpenPin(6);
            pin6.SetDriveMode(GpioPinDriveMode.Output);
            pin5 = gpio.OpenPin(5);
            pin5.SetDriveMode(GpioPinDriveMode.Input);

            makeConnection();
            pin6.Write(GpioPinValue.Low);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        async private void makeConnection()
        {
            try
            {
                var hubConnection = new HubConnection("http://bananasvc.azurewebsites.net");
                chat = hubConnection.CreateHubProxy("ChatHub");
                Context = SynchronizationContext.Current;
                chat.On<string, string>("broadcastMessage",
                    (name, message) =>
                        Context.Post(delegate
                        {
                            if (message.Equals("100"))
                                CallElevator();
                        }, null)
                        );
                await hubConnection.Start();
                //await chat.Invoke("Notify", chatName.Text, hubConnection.ConnectionId);
            }
            catch (Exception ex)
            {

            }
        }
        /*
        private void sendToAzure(ConnectTheDotsSensor sended)
        {
            List<ConnectTheDotsSensor> sensors = new List<ConnectTheDotsSensor> {
                new ConnectTheDotsSensor()
            };
            sensors.Add(sended);

            ctdHelper = new ConnectTheDotsHelper(serviceBusNamespace: "primeiroteste",
                eventHubName: "ehdevices",
                keyName: "Netduino",
                key: "NQKbF6r3t8HPFUaa3wxT0Hq2hfi/+HqsXsdcJlDoW/w=",
                displayName: "NetDuino_SpiderSensor",
                organization: "Build",
                location: "Allianz",
                sensorList: sensors);

            ctdHelper.SendSensorData(sended);
        }*/

        private void Timer_Tick(object sender, object e)
        {
            readPin();
        }

        private void readPin()
        {
            string value = pin5.Read().ToString();
            if (value == "Low")
            {
                if (chamado)
                {
                    observer = true;
                    textStatusREAD.Text = "elevador foi chamado e o LED foi ligado";
                }
            }
            else
            {
                if (observer)
                {
                    observer = false;
                    chamado = false;
                    pin6.Write(GpioPinValue.Low);
                    textStatusREAD.Text = "Elevador CHEGOU CARALHOOO!";
                    sendNotification();
                }
            }
        }
        
        async private void sendNotification()
        {
            try
            {
                await chat.Invoke("Send", "callElevator", "200");

            }
            catch (Exception ex)
            {

            }

        }

        private void CallElevator()
        {
            chamado = true;
            pin6.Write(GpioPinValue.High);
            textStatusREAD.Text = "elevador foi chamado";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CallElevator();
            //textStatusREAD.Text = ":" + pin6.Read().ToString();
        }

      

        private void elevatorIsHere()
        {
            pin6.Write(GpioPinValue.Low);
            textStatus.Text = pin6.Read().ToString();
        }
    }
}
