namespace StreetSignal.Dashboard.Models;
// Exact Firestore strings shared with the mobile application.
public static class Wire {
 public static readonly string[] Services=["water","electricity"];
 public static readonly string[] ReportStatuses=["off","low_pressure","dirty","back_on"];
 public static readonly string[] IncidentStatuses=["suspected","confirmed","resolved"];
 public static readonly string[] PredictionTypes=["water_outage","power_outage","elec_runout","budget_overspend"];
 public static readonly string[] Feedback=["yes","partly","no"];
 public static readonly string[] ReadingKinds=["elec_units"];
 public static readonly string[] AlertSources=["official","predicted","community"];
 public static readonly string[] Billing=["prepaid","postpaid"];
 public static readonly string[] Sources=["app","pilot"];
 public static readonly string[] Sectors=["food","energy","water","transport","prepare"];
 public static readonly string[] PostKinds=["place","offer","request","group","check"];
 public static readonly string[] WaterQuality=["clear","cloudy","smell","unsafe"];
 public static readonly string[] NeedKinds=["power_equipment","mobility","water","young_or_old","other"];
 public static readonly string[] Roles=["resident","leader","official","partner","admin"];
}
