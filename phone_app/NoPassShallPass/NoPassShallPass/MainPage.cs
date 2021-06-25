using Nethereum.HdWallet;
using Nethereum.Signer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace NoPassShallPass
{
    class LoginQrDto
    {
        public string HttpAddress;
        public string Id;
    }

    class LoginRequestDto
    {
        public string Id;
        public string LoginTimestamp;
        public string LoginSignature;
    }

    class MainPage : ContentPage
    {
        private Label labelStatus;
        private Button scanQrButton;
        private Wallet currentWallet;

        public MainPage()
        {
            this.Padding = new Thickness(20, 20, 20, 20);

            StackLayout panel = new StackLayout
            {
                Spacing = 15
            };

            panel.Children.Add(scanQrButton = new Button
            {
                Text = "Scan Login"
            });
            panel.Children.Add(labelStatus = new Label());

            scanQrButton.Clicked += HandleScanQrButtonClickedAsync;

            createWallet();
            this.Content = panel;
        }

        private void createWallet()
        {
            var words = "ripple scissors kick mammal hire column oak again sun offer wealth tomorrow wagon turn fatal";
            var password = "password";

            currentWallet = new Wallet(words, password);
            Console.WriteLine(currentWallet.GetPrivateKey(0));
            Console.WriteLine(currentWallet.GetAccount(0).Address);
        }

        private LoginRequestDto signMessage(int id)
        {
            var currentTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var msg = "0x" + currentTime;

            var signer = new EthereumMessageSigner();
            var signature = signer.EncodeUTF8AndSign(msg, new EthECKey(currentWallet.GetPrivateKey(0), true));

            return new LoginRequestDto
            {
                Id = id.ToString(),
                LoginTimestamp = currentTime,
                LoginSignature = signature
            };
        }

        private async void HandleScanQrButtonClickedAsync(object sender, EventArgs e)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (status == PermissionStatus.Denied)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if ((await Permissions.CheckStatusAsync<Permissions.Flashlight>()) == PermissionStatus.Denied)
            {
                status = await Permissions.RequestAsync<Permissions.Flashlight>();
            }

            if (status == PermissionStatus.Denied)
                return;

            var scanner = new ZXing.Mobile.MobileBarcodeScanner();

            var result = await scanner.Scan();

            var loginQrDto = JsonConvert.DeserializeObject<LoginQrDto>(result.Text);

            var signedMessage = signMessage(int.Parse(loginQrDto.Id));

            var loginDtoJson = new StringContent(JsonConvert.SerializeObject(signedMessage), Encoding.UTF8, "application/json");

            var httpClient = new HttpClient();
            var endpointAddress = new Uri(loginQrDto.HttpAddress);
            var response = await httpClient.PostAsync(endpointAddress, loginDtoJson);

            if (response.IsSuccessStatusCode)
            {
                labelStatus.Text = response.Content.ReadAsStringAsync().Result;
            }
        }
    }
}
