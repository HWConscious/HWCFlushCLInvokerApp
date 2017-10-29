using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using HWC_FlushCLInvokerApp.FlushCLInvokerApp.Models;

namespace HWC_FlushCLInvokerApp
{
    public sealed partial class MainPage : Page
    {
        #region Data members

        private DispatcherTimer _flushConcurrentListsPingTimer { get; set; }
        private DispatcherTimer _mainCloudServicesPingTimer { get; set; }
        private RestClient _flushConcurrentListsRestClient { get; set; }
        private List<RestClient> _mainCloudServicesRestClients { get; set; }

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
            // Initialize REST clients
            RestClientInitialize();

            // Initialize ping timers
            PingTimerInitialize();
        }

        // Initialize the clients for REST calls
        private void RestClientInitialize()
        {
            var requestHeaders = new Dictionary<string, string>() { { "x-api-key", Constants.XApiKeyValue } };

            // Create REST client to ping FlushConcurrentLists
            _flushConcurrentListsRestClient = new RestClient(
                RestClient.HttpVerb.POST,
                Constants.RestApiEndpoint + "/flush_concurrent_lists",
                requestHeaders,
                null,
                null,
                3000);  // 3 seconds timeout
            
            // Create REST clients to ping AddUserLocation, GetDisplayEndpointNotifications and AddDisplayEndpointEvent
            _mainCloudServicesRestClients = new List<RestClient>()
            {
                // REST client for AddUserLocation
                new RestClient(
                    RestClient.HttpVerb.POST,
                    Constants.RestApiEndpoint + "/users/1/locations",   // Pseduo User
                    requestHeaders,
                    null,
                    "{\"Type\": \"IBeacon\", \"DeviceID\": \"pseudo-uuid\"}",   // Pseduo LocationDevice
                    1000),  // 1 second timeout

                // REST client for GetDisplayEndpointNotifications
                new RestClient(
                    RestClient.HttpVerb.GET,
                    Constants.RestApiEndpoint + "/display_endpoints/1/notifications",   // Pseduo DisplayEndpoint
                    requestHeaders,
                    null,
                    null,
                    1000),  // 1 second timeout

                // REST client for AddDisplayEndpointEvent
                new RestClient(
                    RestClient.HttpVerb.POST,
                    Constants.RestApiEndpoint + "/display_endpoints/1/events",   // Pseduo DisplayEndpoint
                    requestHeaders,
                    null,
                    "{\"Type\": \"DisplayEndpoint_Touch\", \"EventAtTimestamp\": \"2017-01-01T00:00:00.000Z\", \"SourceType\": \"Notification\", \"SourceID\": 1, \"Message\": \"\"}",  // Pseduo Notification
                    1000)   // 1 second timeout
            };
        }

        // Initialize ping timers
        private void PingTimerInitialize()
        {
            // Create & start the ping timer for FlushConcurrentLists
            _flushConcurrentListsPingTimer = new DispatcherTimer();
            _flushConcurrentListsPingTimer.Interval = TimeSpan.FromMilliseconds(Constants.FlushConcurrentListsPingFrequencyInMs);
            _flushConcurrentListsPingTimer.Tick += FlushConcurrentListsPingTimer_Tick;
            _flushConcurrentListsPingTimer.Start();         // Start the timer
            FlushConcurrentListsPingTimer_Tick(null, null); // Make a manual tick for the timer

            // Create & start the ping timer for main cloud services
            _mainCloudServicesPingTimer = new DispatcherTimer();
            _mainCloudServicesPingTimer.Interval = TimeSpan.FromMilliseconds(Constants.MainCloudServicesPingFrequencyInMs);
            _mainCloudServicesPingTimer.Tick += MainCloudServicesPingTimer_Tick;
            _mainCloudServicesPingTimer.Start();            // Start the timer
            MainCloudServicesPingTimer_Tick(null, null);    // Make a manual tick for the timer
        }

        #endregion

        #region Private methods

        // FlushConcurrentLists ping timer ticked event
        private void FlushConcurrentListsPingTimer_Tick(object sender, object e)
        {
            PingFlushConcurrentListsAsync();
        }

        // Main cloud services ping timer ticked event
        private void MainCloudServicesPingTimer_Tick(object sender, object e)
        {
            PingMainCloudServicesAsync();
        }

        // Ping FlushConcurrentLists
        private async void PingFlushConcurrentListsAsync()
        {
            try
            {
                // Make REST call to ping FlushConcurrentLists service
                string responseValue = await _flushConcurrentListsRestClient?.MakeRequestAsync();
                string log = "ConcurrentLists flush successful";
                Log.LogAsync(Log.LoggingLevel.Information, log);
                AddLogToFrontendBlock(log);
            }
            catch (Exception ex)
            {
                string log = "Error in REST call for ConcurrentLists flushing." + Environment.NewLine + "EXCEPTION: " + ex.Message;
                Log.LogAsync(Log.LoggingLevel.Error, log);
                AddLogToFrontendBlock(log);
            }
        }

        // Ping all main cloud services
        private async void PingMainCloudServicesAsync()
        {
            if (_mainCloudServicesRestClients?.Any() ?? false)
            {
                // Invoke the set of cloud services 2 times consecutively with interval of 5 seconds, makes the loaded lambda container smoother.
                for (int i = 1; i <= 2; i++)
                {
                    foreach (RestClient restClient in _mainCloudServicesRestClients)
                    {
                        PingAMainCloudServiceAsync(restClient);
                    }
                    await Task.Delay(5000);
                }
            }
        }

        // Ping a main cloud service
        private async void PingAMainCloudServiceAsync(RestClient restClient)
        {
            if (restClient != null)
            {
                string cloudServiceName = string.Empty;
                var index = _mainCloudServicesRestClients.IndexOf(restClient);
                if (index == 0) { cloudServiceName = "1. HWC_AddUserLocation"; }
                else if (index == 1) { cloudServiceName = "2. HWC_GetDisplayEndpointNotifications"; }
                else if (index == 2) { cloudServiceName = "3. HWC_AddDisplayEndpointEvent"; }

                try
                {
                    // Make REST call to ping the cloud service
                    string responseValue = await restClient?.MakeRequestAsync();
                    string log = "Cloud service ping successful [" + cloudServiceName + "]";
                    Log.LogAsync(Log.LoggingLevel.Information, log);
                    AddLogToFrontendBlock(log);
                }
                catch (Exception ex)
                {
                    string log = "Error in REST call for cloud service pinging [" + cloudServiceName + "]" + Environment.NewLine + "EXCEPTION: " + ex.Message;
                    Log.LogAsync(Log.LoggingLevel.Error, log);
                    AddLogToFrontendBlock(log);

                    // Retry the ping after 5 seconds (recursively)
                    await Task.Delay(5000);
                    PingAMainCloudServiceAsync(restClient);
                }
            }
        }

        #endregion

        #region Helper methods

        private void AddLogToFrontendBlock(string log)
        {
            if (!string.IsNullOrEmpty(log))
            {
                _logTextBlock.Text += "[" + DateTime.Now.ToString() + "] " + log + Environment.NewLine;
                _logScrollViewer.ChangeView(null, _logScrollViewer.ExtentHeight, null);
            }
        }

        #endregion
    }
}
