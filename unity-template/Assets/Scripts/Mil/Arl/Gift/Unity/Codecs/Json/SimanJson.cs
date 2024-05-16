using System;
using System.Collections.Generic;
using Mil.Arl.Gift.Unity.Messaging.Incoming;
using UnityEngine;

public class SimanJson : JsonCodec
{
    private const string TYPE_KEY = "Siman_Type";
    private const string ROUTE_KEY = "RouteType";
    private const string LOAD_ARGS_KEY = "LoadArgs";

    public override object Deserialize(JSONObject jObj)
    {
        //Ensure that all expected fields are present
        CheckForExpectedFields(jObj, TYPE_KEY, ROUTE_KEY);

        Debug.Log("Fields are confirmed");

        SimanType simanType = (SimanType) ParseFieldAsEnum(jObj, TYPE_KEY, typeof(SimanType));
        RouteType routeType = (RouteType) ParseFieldAsEnum(jObj, ROUTE_KEY, typeof(RouteType));
        Debug.Log("Strings parsed as enums");
        Dictionary<string, string> loadArgs = jObj.HasField(LOAD_ARGS_KEY) ? ParseFieldAsDictionary(jObj, LOAD_ARGS_KEY) : null;
        Debug.Log("loadArgs = " + (loadArgs == null ? "null" : loadArgs.ToString()));

        return new Siman()
        {
            SimanType = simanType,
            RouteType = routeType,
            LoadArgs = loadArgs
        };
    }

    public override string Serialize(object obj)
    {
        Siman siman = (Siman) obj;
        JSONObject jObj = new JSONObject();
        JSONObject loadArgs = new JSONObject(siman.LoadArgs);

        jObj.AddField(TYPE_KEY, siman.SimanType.ToString());
        jObj.AddField(ROUTE_KEY, siman.RouteType.ToString());
        jObj.AddField(LOAD_ARGS_KEY, loadArgs);

        return jObj.Print();
    }
}