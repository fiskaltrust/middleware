using Android.OS;
using Android.Runtime;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Bindings.Additions;
using Java.Interop;
using System;

namespace DE.Fiskal.Connector.Android.Api.Model
{
    public sealed partial class SelfCheckResponse
    {
        public sealed partial class Creator
        {
            Java.Lang.Object IParcelableCreator.CreateFromParcel(Parcel source) => CreateFromParcel(source);

            Java.Lang.Object[] IParcelableCreator.NewArray(int size) => NewArray(size);
        }

        public sealed partial class Error
        {
            public sealed partial class Creator
            {
                Java.Lang.Object IParcelableCreator.CreateFromParcel(Parcel source) => CreateFromParcel(source);

                Java.Lang.Object[] IParcelableCreator.NewArray(int size) => NewArray(size);
            }
        }

		[Register(".ctor", "(Ljava/lang/String;Ljava/lang/String;Ljava/util/List;Ljava/lang/Long;Ljava/lang/String;Ljava/lang/Long;Ljava/lang/Long;Ljava/lang/String;Ljava/util/List;Ljava/util/List;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/Boolean;Ljava/lang/Long;Ljava/lang/Boolean;Ljava/lang/Boolean;Ljava/lang/Long;Ljava/lang/Long;)V", "")]
		public unsafe SelfCheckResponse(string description, string localClientVersion, global::System.Collections.Generic.IList<ParsablePair> configParameters, global::Java.Lang.Long lastTransactionCounter, string remoteCspVersion, global::Java.Lang.Long cspSystemTimeSeconds, global::Java.Lang.Long resolutionLocalMachineTimeSeconds, string keyReference, global::System.Collections.Generic.IList<global::DE.Fiskal.Connector.Android.Api.Model.SelfCheckTssKeyInfo> keyInfos, global::System.Collections.Generic.IList<global::DE.Fiskal.Connector.Android.Api.Model.SelfCheckResponse.Error> failures, string fccId, string fccVersion, string hwSerialNumberHex, string cspId, global::Java.Lang.Boolean localTimeWarning, global::Java.Lang.Long dbSize, global::Java.Lang.Boolean warningDbSize60Percentage, global::Java.Lang.Boolean warningDbSize90Percentage, global::Java.Lang.Long numberOfClients, global::Java.Lang.Long numberOfOpenTransactions) : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			const string __id = "(Ljava/lang/String;Ljava/lang/String;Ljava/util/List;Ljava/lang/Long;Ljava/lang/String;Ljava/lang/Long;Ljava/lang/Long;Ljava/lang/String;Ljava/util/List;Ljava/util/List;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/Boolean;Ljava/lang/Long;Ljava/lang/Boolean;Ljava/lang/Boolean;Ljava/lang/Long;Ljava/lang/Long;)V";

			if (((global::Java.Lang.Object)this).Handle != IntPtr.Zero)
				return;

			IntPtr native_description = JNIEnv.NewString(description);
			IntPtr native_localClientVersion = JNIEnv.NewString(localClientVersion);
			IntPtr native_configParameters = global::Android.Runtime.JavaList<ParsablePair>.ToLocalJniHandle(configParameters);
			IntPtr native_remoteCspVersion = JNIEnv.NewString(remoteCspVersion);
			IntPtr native_keyReference = JNIEnv.NewString(keyReference);
			IntPtr native_keyInfos = global::Android.Runtime.JavaList<global::DE.Fiskal.Connector.Android.Api.Model.SelfCheckTssKeyInfo>.ToLocalJniHandle(keyInfos);
			IntPtr native_failures = global::Android.Runtime.JavaList<global::DE.Fiskal.Connector.Android.Api.Model.SelfCheckResponse.Error>.ToLocalJniHandle(failures);
			IntPtr native_fccId = JNIEnv.NewString(fccId);
			IntPtr native_fccVersion = JNIEnv.NewString(fccVersion);
			IntPtr native_hwSerialNumberHex = JNIEnv.NewString(hwSerialNumberHex);
			IntPtr native_cspId = JNIEnv.NewString(cspId);
			try
			{
				JniArgumentValue* __args = stackalloc JniArgumentValue[20];
				__args[0] = new JniArgumentValue(native_description);
				__args[1] = new JniArgumentValue(native_localClientVersion);
				__args[2] = new JniArgumentValue(native_configParameters);
				__args[3] = new JniArgumentValue((lastTransactionCounter == null) ? IntPtr.Zero : ((global::Java.Lang.Object)lastTransactionCounter).Handle);
				__args[4] = new JniArgumentValue(native_remoteCspVersion);
				__args[5] = new JniArgumentValue((cspSystemTimeSeconds == null) ? IntPtr.Zero : ((global::Java.Lang.Object)cspSystemTimeSeconds).Handle);
				__args[6] = new JniArgumentValue((resolutionLocalMachineTimeSeconds == null) ? IntPtr.Zero : ((global::Java.Lang.Object)resolutionLocalMachineTimeSeconds).Handle);
				__args[7] = new JniArgumentValue(native_keyReference);
				__args[8] = new JniArgumentValue(native_keyInfos);
				__args[9] = new JniArgumentValue(native_failures);
				__args[10] = new JniArgumentValue(native_fccId);
				__args[11] = new JniArgumentValue(native_fccVersion);
				__args[12] = new JniArgumentValue(native_hwSerialNumberHex);
				__args[13] = new JniArgumentValue(native_cspId);
				__args[14] = new JniArgumentValue((localTimeWarning == null) ? IntPtr.Zero : ((global::Java.Lang.Object)localTimeWarning).Handle);
				__args[15] = new JniArgumentValue((dbSize == null) ? IntPtr.Zero : ((global::Java.Lang.Object)dbSize).Handle);
				__args[16] = new JniArgumentValue((warningDbSize60Percentage == null) ? IntPtr.Zero : ((global::Java.Lang.Object)warningDbSize60Percentage).Handle);
				__args[17] = new JniArgumentValue((warningDbSize90Percentage == null) ? IntPtr.Zero : ((global::Java.Lang.Object)warningDbSize90Percentage).Handle);
				__args[18] = new JniArgumentValue((numberOfClients == null) ? IntPtr.Zero : ((global::Java.Lang.Object)numberOfClients).Handle);
				__args[19] = new JniArgumentValue((numberOfOpenTransactions == null) ? IntPtr.Zero : ((global::Java.Lang.Object)numberOfOpenTransactions).Handle);
				var __r = _members.InstanceMethods.StartCreateInstance(__id, ((object)this).GetType(), __args);
				SetHandle(__r.Handle, JniHandleOwnership.TransferLocalRef);
				_members.InstanceMethods.FinishCreateInstance(__id, this, __args);
			}
			finally
			{
				JNIEnv.DeleteLocalRef(native_description);
				JNIEnv.DeleteLocalRef(native_localClientVersion);
				JNIEnv.DeleteLocalRef(native_configParameters);
				JNIEnv.DeleteLocalRef(native_remoteCspVersion);
				JNIEnv.DeleteLocalRef(native_keyReference);
				JNIEnv.DeleteLocalRef(native_keyInfos);
				JNIEnv.DeleteLocalRef(native_failures);
				JNIEnv.DeleteLocalRef(native_fccId);
				JNIEnv.DeleteLocalRef(native_fccVersion);
				JNIEnv.DeleteLocalRef(native_hwSerialNumberHex);
				JNIEnv.DeleteLocalRef(native_cspId);
				global::System.GC.KeepAlive(configParameters);
				global::System.GC.KeepAlive(lastTransactionCounter);
				global::System.GC.KeepAlive(cspSystemTimeSeconds);
				global::System.GC.KeepAlive(resolutionLocalMachineTimeSeconds);
				global::System.GC.KeepAlive(keyInfos);
				global::System.GC.KeepAlive(failures);
				global::System.GC.KeepAlive(localTimeWarning);
				global::System.GC.KeepAlive(dbSize);
				global::System.GC.KeepAlive(warningDbSize60Percentage);
				global::System.GC.KeepAlive(warningDbSize90Percentage);
				global::System.GC.KeepAlive(numberOfClients);
				global::System.GC.KeepAlive(numberOfOpenTransactions);
			}
		}
	}
}
