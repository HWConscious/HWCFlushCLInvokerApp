using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using HWC_FlushCLInvokerApp.FlushCLInvokerApp.Models;

namespace HWC_FlushCLInvokerApp
{
    public sealed partial class MainPage : Page
    {
        #region Data members

        private DispatcherTimer _flushConcurrentListMicroservicePingTimer { get; set; }
        private DispatcherTimer _regularMicroservicesPingTimer { get; set; }
        private Microservice _flushConcurrentListMicroservice { get; set; }
        private List<Microservice> _regularMicroservices { get; set; }
        private int? _totalPingCount { get; set; }
        private SolidColorBrush _brushBlack = new SolidColorBrush(Colors.Black);
        private SolidColorBrush _brushGreen = new SolidColorBrush(Colors.Green);
        private SolidColorBrush _brushYellow = new SolidColorBrush(Colors.Orange);
        private SolidColorBrush _brushRed = new SolidColorBrush(Colors.Red);

        #endregion

        #region Initialize

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.LogAsync(Log.LoggingLevel.Information, "Navigated to Main Page");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize all Microservices
            MicroserviceInitialize();

            // Initialize ping timers
            PingTimerInitialize();
        }

        // Initialize Microservices
        private void MicroserviceInitialize()
        {
            var requestHeaders = new Dictionary<string, string>() { { "x-api-key", Constants.XApiKeyValue } };
            
            _flushConcurrentListMicroservice = new Microservice("HWC_FlushConcurrentLists",
                // REST client for FlushConcurrentLists
                new RestClient(
                    RestClient.HttpVerb.POST,
                    Constants.RestApiEndpoint + "/flush_concurrent_lists",
                    requestHeaders,
                    null,
                    null,
                    3000  // 3 seconds timeout
                )
            );

            _regularMicroservices = new List<Microservice>()
            {
                new Microservice(
                    "HWC_AddUserLocation",
                    // REST client for AddUserLocation
                    new RestClient(
                        RestClient.HttpVerb.POST,
                        Constants.RestApiEndpoint + "/users/1/locations",   // Pseduo User
                        requestHeaders,
                        null,
                        "{\"Type\": \"IBeacon\", \"DeviceID\": \"pseudo-uuid\"}",   // Pseduo LocationDevice
                        1000  // 1 second timeout
                    )
                ),
                new Microservice(
                    "HWC_AddMovingBeaconLocation",
                    // REST client for AddMovingBeaconLocation
                    new RestClient(
                        RestClient.HttpVerb.POST,
                        Constants.RestApiEndpoint + "/location_devices/locations",
                        requestHeaders,
                        null,
                        "{\"Type\": \"IBeacon\", \"DeviceID\": \"pseudo-uuid\"}",   // Pseduo LocationDevice
                        1000  // 1 second timeout
                    )
                ),
                new Microservice(
                    "HWC_GetDisplayEndpointNotifications",
                    // REST client for GetDisplayEndpointNotifications
                    new RestClient(
                        RestClient.HttpVerb.GET,
                        Constants.RestApiEndpoint + "/display_endpoints/1/notifications",   // Pseduo DisplayEndpoint
                        requestHeaders,
                        null,
                        null,
                        1000  // 1 second timeout
                    )
                ),
                new Microservice(
                    "HWC_AddDisplayEndpointEvent",
                    // REST client for AddDisplayEndpointEvent
                    new RestClient(
                        RestClient.HttpVerb.POST,
                        Constants.RestApiEndpoint + "/display_endpoints/1/events",   // Pseduo DisplayEndpoint
                        requestHeaders,
                        null,
                        "{\"Type\": \"DisplayEndpoint_Touch\", \"EventAtTimestamp\": \"2017-01-01T00:00:00.000Z\", \"SourceType\": \"Notification\", \"SourceID\": 1, \"Message\": \"\"}",  // Pseduo Notification
                        1000   // 1 second timeout
                    )
                )
            };

            _totalPingCount = ((_regularMicroservices?.Count ?? 0) + 1) * Constants.RegularMicroservicesConsecutiveCallNoOfTimes;
        }

        // Initialize ping timers
        private void PingTimerInitialize()
        {
            // Create & start the ping timer for FlushConcurrentLists
            _flushConcurrentListMicroservicePingTimer = new DispatcherTimer();
            _flushConcurrentListMicroservicePingTimer.Interval = TimeSpan.FromMilliseconds(Constants.FlushConcurrentListsMicroservicePingFrequencyInMs);
            _flushConcurrentListMicroservicePingTimer.Tick += FlushConcurrentListMicroservicePingTimer_Tick;
            _flushConcurrentListMicroservicePingTimer.Start();         // Start the timer
            FlushConcurrentListMicroservicePingTimer_Tick(null, null); // Make a manual tick for the timer

            // Create & start the ping timer for regular Microservices
            _regularMicroservicesPingTimer = new DispatcherTimer();
            _regularMicroservicesPingTimer.Interval = TimeSpan.FromMilliseconds(Constants.RegularMicroservicesPingFrequencyInMs);
            _regularMicroservicesPingTimer.Tick += RegularMicroservicesPingTimer_Tick;
            _regularMicroservicesPingTimer.Start();            // Start the timer
            RegularMicroservicesPingTimer_Tick(null, null);    // Make a manual tick for the timer
        }

        #endregion

        #region Private methods

        // FlushConcurrentLists Microservice ping timer ticked event
        private void FlushConcurrentListMicroservicePingTimer_Tick(object sender, object e)
        {
            SetEventLogCaption();
            PingFlushConcurrentListsAsync();
        }

        // Regular Microservices ping timer ticked event
        private void RegularMicroservicesPingTimer_Tick(object sender, object e)
        {
            _flushConcurrentListMicroservice?.ResetPingCounter();
            _regularMicroservices?.ForEach(rM => rM.ResetPingCounter());

            SetEventLogCaption();
            PingRegularMicroservicesAsync();
        }

        // Ping FlushConcurrentLists
        private async void PingFlushConcurrentListsAsync()
        {
            if (_flushConcurrentListMicroservice != null)
            {
                try
                {
                    // Make REST call to ping FlushConcurrentLists Microservice
                    var responseValue = await _flushConcurrentListMicroservice.RestClient?.MakeRequestAsync();
                    _flushConcurrentListMicroservice.PingCounter += 1;

                    var loggingLevel = _flushConcurrentListMicroservice.PingCounter < Constants.RegularMicroservicesConsecutiveCallNoOfTimes ? Log.LoggingLevel.Warning : Log.LoggingLevel.Information;
                    var log = "ConcurrentLists flush successful";
                    Log.LogAsync(loggingLevel, log);
                    AddLogToFrontendBlock(loggingLevel, log);
                }
                catch (Exception ex)
                {
                    var log = "Error in REST call for ConcurrentLists flushing." + Environment.NewLine + "EXCEPTION: " + ex.Message;
                    Log.LogAsync(Log.LoggingLevel.Error, log);
                    AddLogToFrontendBlock(Log.LoggingLevel.Error, log);
                }
            }
        }

        // Ping all regular Microservices
        private async void PingRegularMicroservicesAsync()
        {
            if (_regularMicroservices?.Any() ?? false)
            {
                // Invoke the set of Microservices 'n' times consecutively with interval of 'n' seconds, makes the loaded lambda container smoother.
                for (int i = 1; i <= Constants.RegularMicroservicesConsecutiveCallNoOfTimes; i++)
                {
                    _regularMicroservices.ForEach(regularMicroservice => PingARegularMicroserviceAsync(regularMicroservice));
                    await Task.Delay(Constants.RegularMicroservicesConsecutiveCallsIntervalInMs);
                }
            }
        }

        // Ping a regular Microservice
        private async void PingARegularMicroserviceAsync(Microservice microservice)
        {
            if (microservice != null)
            {
                var microserviceName = ((_regularMicroservices?.FindIndex(rM => rM.ServiceName.Equals(microservice.ServiceName)) ?? 0) + 1) + ". " + microservice.ServiceName;
                try
                {
                    // Make REST call to ping the Microservice
                    var responseValue = await microservice.RestClient?.MakeRequestAsync();
                    microservice.PingCounter += 1;

                    var loggingLevel = microservice.PingCounter < Constants.RegularMicroservicesConsecutiveCallNoOfTimes ? Log.LoggingLevel.Warning : Log.LoggingLevel.Information;
                    var log = "Microservice ping successful [" + microserviceName + "]";
                    Log.LogAsync(loggingLevel, log);
                    AddLogToFrontendBlock(loggingLevel, log);
                }
                catch (Exception ex)
                {
                    var log = "Error in REST call for Microservice pinging [" + microserviceName + "]" + Environment.NewLine + "EXCEPTION: " + ex.Message;
                    Log.LogAsync(Log.LoggingLevel.Error, log);
                    AddLogToFrontendBlock(Log.LoggingLevel.Error, log);

                    // Retry the ping after 'n' seconds (recursively)
                    await Task.Delay(Constants.RegularMicroservicesConsecutiveCallsIntervalInMs);
                    PingARegularMicroserviceAsync(microservice);
                }
            }
        }

        #endregion

        #region Helper methods

        private void AddLogToFrontendBlock(Log.LoggingLevel loggingLevel, string log)
        {
            if (!string.IsNullOrEmpty(log))
            {
                var run = new Run()
                {
                    Text = "[" + DateTime.Now.ToString() + "] " + log + Environment.NewLine,
                    Foreground = _brushBlack
                };
                switch (loggingLevel)
                {
                    case Log.LoggingLevel.Information:
                        run.Foreground = _brushGreen;
                        break;
                    case Log.LoggingLevel.Warning:
                        run.Foreground = _brushYellow;
                        break;
                    case Log.LoggingLevel.Error:
                    case Log.LoggingLevel.Critical:
                        run.Foreground = _brushRed;
                        break;
                }
                _logTextBlock.Inlines.Add(run);

                _logScrollViewer.ChangeView(null, _logScrollViewer.ExtentHeight, null);
            }
        }

        private void SetEventLogCaption()
        {
            var currentPingCount = (_regularMicroservices?.Sum(rM => rM.PingCounter) ?? 0) + (_flushConcurrentListMicroservice?.PingCounter ?? 0);

            if (currentPingCount < _totalPingCount)
            {
                _eventLogCaption.Text = "[Warming up Microservices...]";
                _eventLogCaption.Foreground = _brushRed;
            }
            else
            {
                _eventLogCaption.Text = "[Microservices warmed up]";
                _eventLogCaption.Foreground = _brushGreen;
            }
        }

        #endregion
    }

    public class Microservice
    {
        public readonly string ServiceName;
        public readonly RestClient RestClient;
        public int PingCounter;

        public Microservice(string serviceName, RestClient restClient)
        {
            ServiceName = serviceName;
            RestClient = restClient;
            PingCounter = 0;
        }

        public void ResetPingCounter()
        {
            PingCounter = 0;
        }
    }
}
