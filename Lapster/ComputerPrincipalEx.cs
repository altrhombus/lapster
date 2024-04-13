using ActiveDs;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using static Lapster.Win32;

namespace Lapster
{
    internal class Win32
    {
        [Flags]
        public enum ProtectFlags
        {
            NCRYPT_SILENT_FLAG = 0x00000040,
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate int PFNCryptStreamOutputCallback(IntPtr pvCallbackCtxt, IntPtr pbData, int cbData, [MarshalAs(UnmanagedType.Bool)] bool fFinal);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct NCRYPT_PROTECT_STREAM_INFO
        {
            public PFNCryptStreamOutputCallback pfnStreamOutput;
            public IntPtr pvCallbackCtxt;
        }

        [Flags]
        public enum UnprotectSecretFlags
        {
            NCRYPT_UNPROTECT_NO_DECRYPT = 0x00000001,
            NCRYPT_SILENT_FLAG = 0x00000040,
        }

        [DllImport("ncrypt.dll")]
        public static extern uint NCryptStreamOpenToUnprotect(in NCRYPT_PROTECT_STREAM_INFO pStreamInfo, ProtectFlags dwFlags, IntPtr hWnd, out IntPtr phStream);

        [DllImport("ncrypt.dll")]
        public static extern uint NCryptStreamUpdate(IntPtr hStream, IntPtr pbData, int cbData, [MarshalAs(UnmanagedType.Bool)] bool fFinal);

        [DllImport("ncrypt.dll")]
        public static extern uint NCryptUnprotectSecret(out IntPtr phDescriptor, Int32 dwFlags, IntPtr pbProtectedBlob, uint cbProtectedBlob, IntPtr pMemPara, IntPtr hWnd, out IntPtr ppbData, out uint pcbData);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        public static extern uint NCryptGetProtectionDescriptorInfo(IntPtr hDescriptor, IntPtr pMemPara, int dwInfoType, out string ppvInfo);

    }

    [DirectoryRdnPrefix("CN")]
    [DirectoryObjectClass("Computer")]
    public class ComputerPrincipalEx : ComputerPrincipal
    {
        public ComputerPrincipalEx(PrincipalContext context) : base(context) { }

        public ComputerPrincipalEx(PrincipalContext context, string samAccountName, string password, bool enabled) : base(context, samAccountName, password, enabled) { }

        public static new ComputerPrincipalEx FindByIdentity(PrincipalContext ctx, string identityValue)
        {
            return (ComputerPrincipalEx)FindByIdentityWithType(ctx, typeof(ComputerPrincipalEx), identityValue);
        }

        [DirectoryProperty("ms-Mcs-AdmPwd")]
        public string MsMcsAdmPwd
        {
            get
            {
                if (ExtensionGet("ms-Mcs-AdmPwd").Length != 1) return string.Empty;
                return (string)ExtensionGet("ms-Mcs-AdmPwd")[0];
            }
            set { ExtensionSet("ms-Mcs-AdmPwd", value); }
        }

        [DirectoryProperty("ms-Mcs-AdmPwdExpirationTime")]
        public long MsMcsAdmPwdExpirationTime
        {
            get
            {
                if (ExtensionGet("ms-Mcs-AdmPwdExpirationTime").Length != 1) return 0;
                var comObject = ExtensionGet("ms-Mcs-AdmPwdExpirationTime")[0] as IADsLargeInteger;
                if (comObject != null)
                {
                    // IADsLargeInteger is an interface for handling large integers in Active Directory.
                    // It has two properties: HighPart and LowPart.
                    long high = (long)comObject.HighPart;
                    long low = (uint)comObject.LowPart;
                    // Combine the high and low parts to form the long value.
                    return (high << 32) | low;
                }
                throw new InvalidCastException("The ms-Mcs-AdmPwdExpirationTime property cannot be cast to IADsLargeInteger.");
            }
            set
            {
                var largeInt = (IADsLargeInteger)Activator.CreateInstance(Type.GetTypeFromProgID("LargeInteger"));
                largeInt.HighPart = (int)(value >> 32);
                largeInt.LowPart = (int)(value & 0xFFFFFFFF);
                ExtensionSet("ms-Mcs-AdmPwdExpirationTime", largeInt);
            }
        }

        [DirectoryProperty("msLAPS-EncryptedDSRMPassword")]
        public byte[]? MsLapsEncryptedDsrmPassword
        {
            get
            {
                if (ExtensionGet("msLAPS-EncryptedDSRMPassword").Length != 1) return null;
                return (byte[])ExtensionGet("msLAPS-EncryptedDSRMPassword")[0];
            }
            set { ExtensionSet("msLAPS-EncryptedDSRMPassword", value); }
        }

        [DirectoryProperty("msLAPS-EncryptedDSRMPasswordHistory")]
        public byte[][]? MsLapsEncryptedDsrmPasswordHistory
        {
            get
            {
                if (ExtensionGet("msLAPS-EncryptedDSRMPasswordHistory").Length != 1) return null;

                byte[][] result = new byte[ExtensionGet("msLAPS-EncryptedDSRMPasswordHistory").Length][];
                for (int i = 0; i < ExtensionGet("ms-LAPS-EncryptedDSRMPasswordHistory").Length; i++)
                {
                    result[i] = (byte[])ExtensionGet("ms-LAPS-EncryptedDSRMPasswordHistory")[i];
                }
                return result;
            }
            set { ExtensionSet("msLAPS-EncryptedDSRMPasswordHistory", value); }
        }

        [DirectoryProperty("msLAPS-EncryptedPassword")]
        public string MsLapsEncryptedPassword
        {
            get
            {
                if (ExtensionGet("msLAPS-EncryptedPassword").Length != 1) return string.Empty;
                return DecryptByte((byte[])ExtensionGet("msLAPS-EncryptedPassword")[0]);
            }
            set { ExtensionSet("msLAPS-EncryptedPassword", value); }
        }

        [DirectoryProperty("msLAPS-EncryptedPasswordHistory")]
        public string[]? MsLapsEncryptedPasswordHistory
        {
            get
            {
                var extensionData = ExtensionGet("msLAPS-EncryptedPasswordHistory");
                if (extensionData.Length == 0) return null;
                string[] result = new string[extensionData.Length];
                for (int i = 0; i < extensionData.Length; i++)
                {
                    result[i] = DecryptByte((byte[])extensionData[i]);
                }
                return result;
            }
            set { ExtensionSet("msLAPS-EncryptedPasswordHistory", value); }
        }

        [DirectoryProperty("msLAPS-Password")]
        public string MsLapsPassword
        {
            get
            {
                if (ExtensionGet("msLAPS-Password").Length != 1) return string.Empty;
                return (string)ExtensionGet("msLAPS-Password")[0];
            }
            set { ExtensionSet("msLAPS-Password", value); }
        }

        [DirectoryProperty("msLAPS-PasswordExpirationTime")]
        public long MsLapsPasswordExpirationTime
        {
            get
            {
                if (ExtensionGet("msLAPS-PasswordExpirationTime").Length != 1) return 0;
                var comObject = ExtensionGet("msLAPS-PasswordExpirationTime")[0] as IADsLargeInteger;
                if (comObject != null)
                {
                    // IADsLargeInteger is an interface for handling large integers in Active Directory.
                    // It has two properties: HighPart and LowPart.
                    long high = (long)comObject.HighPart;
                    long low = (uint)comObject.LowPart;
                    // Combine the high and low parts to form the long value.
                    return (high << 32) | low;
                }
                throw new InvalidCastException("The msLAPS-PasswordExpirationTime property cannot be cast to IADsLargeInteger.");
            }
            set
            {
                var largeInt = (IADsLargeInteger)Activator.CreateInstance(Type.GetTypeFromProgID("LargeInteger"));
                largeInt.HighPart = (int)(value >> 32);
                largeInt.LowPart = (int)(value & 0xFFFFFFFF);
                ExtensionSet("msLAPS-PasswordExpirationTime", largeInt);
            }
        }


        // Function written by Adam Chester (https://blog.xpnsec.com/lapsv2-internals/)
        // GitHub: https://github.com/xpn/RandomTSScripts/tree/master/lapsv2decrypt
        public string DecryptByte(byte[] encryptedByte)
        {
            string decryptedString = string.Empty;

            Win32.NCRYPT_PROTECT_STREAM_INFO info = new NCRYPT_PROTECT_STREAM_INFO
            {
                pfnStreamOutput = new PFNCryptStreamOutputCallback((IntPtr pvCallbackCtxt, IntPtr pbData, int cbData, bool fFinal) =>
                {
                    byte[] data = new byte[cbData];
                    Marshal.Copy(pbData, data, 0, cbData);
                    string str = Encoding.Unicode.GetString(data);
                    decryptedString = str;
                    return 0;
                }),
                pvCallbackCtxt = IntPtr.Zero
            };

            IntPtr handle;
            IntPtr handle2;
            IntPtr secData;
            uint secDataLen;

            uint ret = Win32.NCryptStreamOpenToUnprotect(info, ProtectFlags.NCRYPT_SILENT_FLAG, IntPtr.Zero, out handle);
            if (ret == 0)
            {
                IntPtr alloc = Marshal.AllocHGlobal(encryptedByte.Length);
                Marshal.Copy(encryptedByte, 16, alloc, encryptedByte.Length - 16);

                ret = Win32.NCryptUnprotectSecret(out handle2, 0x41, alloc, (uint)encryptedByte.Length - 16, IntPtr.Zero, IntPtr.Zero, out secData, out secDataLen);
                ret = Win32.NCryptStreamUpdate(handle, alloc, encryptedByte.Length - 16, true);
            }

            return decryptedString;
        }
    }
}