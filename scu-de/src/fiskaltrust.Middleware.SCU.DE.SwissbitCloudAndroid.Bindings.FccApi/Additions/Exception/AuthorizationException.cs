﻿using Android.OS;

namespace DE.Fiskal.Connector.Android.Api.Exception
{
    public sealed partial class AuthorizationException
	{
		public new sealed partial class Creator
        {
            Java.Lang.Object IParcelableCreator.CreateFromParcel(Parcel source) => source;

            Java.Lang.Object[] IParcelableCreator.NewArray(int size) => new Java.Lang.Object[size];
        }
	}
}
