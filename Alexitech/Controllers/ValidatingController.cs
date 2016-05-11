using Alexitech.Model;
using Alexitech.Models;
using Alexitech.Scoping;
using HarmonyHub;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace Alexitech.Controllers
{
    public class ValidatingController : Controller
    {
        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            bool success = false;

            var ctx = filterContext.HttpContext;
            if (ctx != null)
            {
                var request = ctx.Request;
                if (request != null && request.Headers != null)
                {
                    string signature = request.Headers["Signature"];
                    string signatureCertChainUrl = request.Headers["SignatureCertChainUrl"];
                    if (!string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(signatureCertChainUrl))
                    {
                        Uri signatureCertChainUri = new Uri(signatureCertChainUrl);
                        if (IsSignatureCertChainUriValid(signatureCertChainUri))
                        {
                            var signatureCertChain = GetSignatureCertChain(signatureCertChainUri);
                            if (signatureCertChain != null)
                            {
                                if (IsRequestSignatureValid(signatureCertChain, signature, request))
                                    success = true;
                            }
                        }
                    }
                }
            }

            if (!success)
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            base.OnAuthorization(filterContext);
        }

        private bool IsSignatureCertChainUriValid(Uri uri)
        {
            if (uri.Scheme != "https")
                return false;

            if (uri.Host != "s3.amazonaws.com")
                return false;

            if (!uri.AbsolutePath.StartsWith("/echo.api/"))
                return false;

            if (uri.Port != 443)
                return false;

            return true;
        }

        private X509Certificate GetSignatureCertChain(Uri uri)
        {
            return ScopingModule.Application.Ensure<X509Certificate>(uri.AbsoluteUri, () =>
            {
                using (var client = new HttpClient())
                {
                    var content = client.GetStringAsync(uri).Result;
                    if (!string.IsNullOrEmpty(content))
                    {
                        var pemReader = new PemReader(new StringReader(content));
                        var cert = (X509Certificate)pemReader.ReadObject();
                        try
                        {
                            cert.CheckValidity();

                            var domainPresent = cert.GetSubjectAlternativeNames()
                                .OfType<ArrayList>()
                                .Any(o => o.OfType<string>().Any(p => p == "echo-api.amazon.com"));

                            if (domainPresent)
                                return cert;
                        }
                        catch (CertificateExpiredException)
                        {
                            return null;
                        }
                        catch (CertificateNotYetValidException)
                        {
                            return null;
                        }
                    }
                }

                return null;
            });
        }

        private bool IsRequestSignatureValid(X509Certificate cert, string signature, HttpRequestBase request)
        {
            byte[] requestBytes;
            using (MemoryStream mem = new MemoryStream())
            {
                request.InputStream.CopyTo(mem);
                request.InputStream.Position = 0;
                requestBytes = mem.ToArray();
            }

            byte[] signatureBytes = null;
            try
            {
                signatureBytes = Convert.FromBase64String(signature);
            }
            catch (FormatException)
            {
                return false;
            }

            var publicKey = (RsaKeyParameters)cert.GetPublicKey();
            var signer = SignerUtilities.GetSigner("SHA1withRSA");
            signer.Init(false, publicKey);
            signer.BlockUpdate(requestBytes, 0, requestBytes.Length);

            return signer.VerifySignature(signatureBytes);
        }
    }
}