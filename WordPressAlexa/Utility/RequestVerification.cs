using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WordPressAlexa.Utility
{
    public static class RequestVerification
    {
        public static async Task<bool> Verify(string encodedSignature, Uri certificatePath, string body)
        {
            if (!VerifyCertificateUrl(certificatePath))
                return false;

            var certificate = await GetCertificate(certificatePath);
            if (!ValidSigningCertificate(certificate) || !VerifyChain(certificate))
                return false;

            if (!AssertHashMatch(certificate, encodedSignature, body))
                return false;

            return true;
        }

        public static bool AssertHashMatch(X509Certificate2 certificate, string encodedSignature, string body)
        {
            byte[] signature;
            try
            {
                signature = Convert.FromBase64String(encodedSignature);
            }
            catch
            {
                return false;
            }
            var rsa = certificate.GetRSAPublicKey();

            return rsa.VerifyData(Encoding.UTF8.GetBytes(body), signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        public static async Task<X509Certificate2> GetCertificate(Uri certificatePath)
        {
            var response = await new HttpClient().GetAsync(certificatePath);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return new X509Certificate2(bytes);
        }

        public static bool VerifyChain(X509Certificate2 certificate)
        {
            // see https://stackoverflow.com/questions/24618798/automated-downloading-of-x509-certificatePath-chain-from-remote-host
            //If you do not provide revokation information, use the following line.
            var certificateChain = new X509Chain { ChainPolicy = { RevocationMode = X509RevocationMode.NoCheck } };
            return certificateChain.Build(certificate);
        }

        private static bool ValidSigningCertificate(X509Certificate2 certificate)
        {
            return DateTime.Now < certificate.NotAfter && DateTime.Now > certificate.NotBefore && certificate.GetNameInfo(X509NameType.SimpleName, false) == "echo-api.amazon.com";
        }

        public static bool VerifyCertificateUrl(Uri certificate)
        {
            return certificate.Scheme == "https" && certificate.Host == "s3.amazonaws.com" && certificate.LocalPath.StartsWith("/echo.api") && certificate.IsDefaultPort;
        }
    }
}