$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=AppPublisher" -KeyUsage DigitalSignature -FriendlyName "KiteCoreDevCert" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
$pwd = ConvertTo-SecureString "" -AsPlainText -Force
Export-PfxCertificate -Cert $cert -FilePath "KiteCoreDevCert.pfx" -Password $pwd
