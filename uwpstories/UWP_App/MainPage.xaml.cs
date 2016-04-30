using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.WindowsAzure.MobileServices;
using UWP_App.Models;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MobileServiceClient client;
        public MobileServiceUser user;
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            client = new MobileServiceClient("https://uwpstories.azurewebsites.net");
            
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            try
            {
                switch (button.Content as string)
                {
                    case "Facebook":
                        user = await client.LoginAsync(MobileServiceAuthenticationProvider.Facebook);
                        break;
                    case "Google":
                        user = await client.LoginAsync(MobileServiceAuthenticationProvider.Google);
                        break;
                    case "Microsoft Account":
                        user = await client.LoginAsync(MobileServiceAuthenticationProvider.MicrosoftAccount);
                        break;
                    case "Twitter":
                        user = await client.LoginAsync(MobileServiceAuthenticationProvider.Twitter);
                        break;
                }
            }
            catch (Exception)
            {

            }
            
            if (user != null)
            {
                var userInfo = await client.InvokeApiAsync<UserInfo>("UserInfo",HttpMethod.Get,null);

                userName.Text = userInfo.Name;

                var httpclient = new HttpClient();
                var bytes = await httpclient.GetByteArrayAsync(userInfo.ImageUri);
                var bi = new BitmapImage();
                await
                    bi.SetSourceAsync(
                        new MemoryStream(bytes).AsRandomAccessStream());

                userImage.Source = bi;
            }
        }
    }
}
