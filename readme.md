### C# dotnet backend
1. Because we are using HttpListener we need to grant permission; 
2. Run as admin: netsh http add urlacl url=http://localhost:666/ user=your_user_name
    * More info: https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/configuring-http-and-https?redirectedfrom=MSDN
    * Add CORS support
    * Add port: netsh advfirewall firewall add rule name= "Open Port 666" dir=in action=allow protocol=TCP localport=666 more info: https://docs.microsoft.com/en-US/troubleshoot/windows-server/networking/netsh-advfirewall-firewall-control-firewall-behavior
2. Frontend:
    * Request qr login image and show it
3. Phone app (make sure you're on same network, so much wasted time...)
    * Enable development mode on your android: https://tweaklibrary.com/how-to-enable-developer-mode-on-android/ and connect it through USB, Visual Studio should see it in Build options
    * Install Xmarin SDK for Android
    * Create new Xmarin Project
    * Add nuget packages: ZXing.Net.Mobile, ZXing.Net.Mobile.Forms, Newtonsoft.JSON, Nethereum.HdWallet 2.1.0 !!! Nethereum.Signer 2.1.0 !!! (got some errors with new versions)
    * Add permission to make http requests and add user-permission for CAMERA and FLASHLIGHT (may not be needed) to AndroidManifest.xml
        * <project_name>.Properties -> AndroidManifest.xml add: android:networkSecurityConfig="@xml/network_security_config" into application as an attribute like android:label
        * to <project_name.Android>.Resources/xml (create new folder)/network_security_config.xml and under domain change IP so it matches backend's ip (my case: 192.168.0.103)
        * on network_security_config right click then check that Build Action is set to AndroidResource (otherwise I will not find/register this config or something and build will fail)
        * <project_name.Android>->MainActivity.cs add ZXing init to OnCreate and add permission handler to OnRequestPermissionsResult
    * Delete existing MainPage and create new item MainPage.cs
    * We need to create qr scanner
    * We need to create wallet and we will it to sign message and we assume that registration already happened and backend knows this public key so it can verify user
    * We need to sign some message and send back

# Session mechanism
1. Frontend sends /requestlogin which will return qr and temporary unique session id
2. Backend registers that session id so upon request it knows which one to authenticate
3. Frontend sends /checksession with that temporary unique id, if login happened successfully it will return token (like json web token)

# Troubleshooting
Cleartext HTTP traffic to 192.168.0.104 not permitted - change <domain includeSubdomains="true"> your IP </domain> in network_security_config.xml 