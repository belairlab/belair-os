using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.Applications;
// The vendor boot unit expects this application to accept its preload request.
// Acknowledge it and exit; interactive requests open the existing Quiet Home process.
public sealed class QuickLaunch : NUIApplication {
 sealed class WeatherDay { public string Date="",High="",Low="",Condition=""; public int Code=-1; }
 sealed class WeatherHour { public string Time="",Temperature="--°",FeelsLike="--°",Precipitation="--%",Wind="--",Day=""; public int Code=-1; }
 sealed class WeatherSummary { public int Code=-1; public string Temperature="--°",Condition="Indisponible",High="",Low=""; public List<WeatherDay> Days=new List<WeatherDay>(); public List<WeatherHour> Hours=new List<WeatherHour>(); }
 sealed class MetroDeparture { public string Destination="",Time=""; public int Minutes; public bool Realtime,AtStop; }
 sealed class MetroSummary { public string Updated="",Source=""; public bool Available; public List<MetroDeparture> Next=new List<MetroDeparture>(); }
 sealed class TrafficSummary { public string Updated="",Header="",Description="",Effect=""; public bool Available,HasAlert; }
 sealed class NewsArticle { public string Title="",Summary="",Url="",Published="",Source="",Image=""; }
 sealed class MarketQuote { public string Symbol="",Name=""; public double Price,Previous; public long Time; public List<double> Points=new List<double>(); public List<long> Times=new List<long>(); }
 sealed class NewsSection { public string Name=""; public List<NewsArticle> Items=new List<NewsArticle>(); }
 sealed class NewsSummary { public string Updated=""; public List<NewsSection> Sections=new List<NewsSection>(); }
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_create(out IntPtr c);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_set_app_id(IntPtr c,string id);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_set_operation(IntPtr c,string op);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_add_extra_data(IntPtr c,string key,string val);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_send_launch_request(IntPtr c,IntPtr cb,IntPtr data);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_destroy(IntPtr c);
 [DllImport("libnotification.so.0")] static extern int notification_status_message_post(string m);
 [DllImport("libnotification.so.0")] static extern IntPtr notification_create(int type);
 [DllImport("libnotification.so.0")] static extern int notification_set_text(IntPtr n,int type,string text,string key,int argsType);
 [DllImport("libnotification.so.0")] static extern int notification_set_display_applist(IntPtr n,int list);
 [DllImport("libnotification.so.0")] static extern int notification_post(IntPtr n);
 [DllImport("libnotification.so.0")] static extern int notification_free(IntPtr n);
 // Banners: a small notification-level window over any input (Apple TV on HDMI included). It stays until OK or Back:
 // only those two keys are grabbed (topmost) while it is shown, every other key keeps going to the Apple TV.
 Window banner;readonly Queue<string[]> bannerQueue=new Queue<string[]>();bool bannerHeld;Tizen.NUI.Timer bannerTimer;
 // Return and Back from the TV remote; Mute also arrives from the Apple TV's remote (it drives the TV's sound).
 const int keyReturn=36,keyBack=166,keyMute=121;static volatile bool closeRequested,bannerVisible;static DateTime ignoreAtvUntil=DateTime.MinValue;
 void ShowBanner(string title,string body,string colour="#669EFF"){
  ShowBanner(title,body,colour,"info");
 }
 void ShowBanner(string title,string body,string colour,string icon,string timeout="0"){
  bannerQueue.Enqueue(new[]{title,body,colour,icon,timeout});
  if(banner==null)NextBanner();
 }
 void NextBanner(){
  if(bannerQueue.Count==0)return;
  var b=bannerQueue.Dequeue();
  try{
   int w=820,h=176;string pic=b.Length>3?BannerPicture(b[3]):"";bool photo=pic!=""&&!pic.EndsWith(".svg");
   banner=new Window("BelAirBanner",new Rectangle(1920-w-48,48,w,h),true);
   banner.Type=WindowType.Notification;
   banner.SetNotificationLevel(NotificationLevel.High);
   banner.SetAcceptFocus(false);
   banner.BackgroundColor=new Color(0,0,0,0);
   var panel=new View{Size2D=new Size2D(w,h),BackgroundColor=new Color(.08f,.10f,.16f,.95f),CornerRadius=new Vector4(28,28,28,28),BorderlineWidth=2,BorderlineColor=new Color(1,1,1,.12f)};
   int pw=photo?172:112,tx=24+pw+22;
   var tile=new View{Position2D=new Position2D(24,(h-112)/2),Size2D=new Size2D(pw,112),CornerRadius=new Vector4(22,22,22,22),BackgroundColor=photo?new Color(0,0,0,.3f):new Color(b[2])};panel.Add(tile);
   if(pic!="")tile.Add(new ImageView{ResourceUrl=pic,Position2D=new Position2D(photo?0:18,photo?0:18),Size2D=new Size2D(photo?pw:76,photo?112:76),FittingMode=photo?FittingModeType.ScaleToFill:FittingModeType.ShrinkToFit,CornerRadius=photo?new Vector4(22,22,22,22):new Vector4(0,0,0,0)});
   panel.Add(new TextLabel{Text=b[0],Position2D=new Position2D(tx,16),Size2D=new Size2D(w-tx-28,48),PixelSize=30,TextColor=Color.White,VerticalAlignment=VerticalAlignment.Center});
   panel.Add(new TextLabel{Text=b[1],Position2D=new Position2D(tx,64),Size2D=new Size2D(w-tx-28,70),PixelSize=23,TextColor=new Color(1,1,1,.80f),MultiLine=true,VerticalAlignment=VerticalAlignment.Top});
   var hint=new TextLabel{Text=b.Length>4&&b[4]!="0"&&b[4]!=""?"":"OK ou clic central Apple TV pour fermer",Position2D=new Position2D(tx,h-40),Size2D=new Size2D(w-tx-28,28),PixelSize=18,TextColor=new Color(1,1,1,.5f),HorizontalAlignment=HorizontalAlignment.End};panel.Add(hint);
   banner.Add(panel);
   banner.KeyEvent+=(o,e)=>{if(e.Key.State!=Key.StateType.Down)return;string k=e.Key.KeyPressedName;Tlog("banner key "+k);if(k=="Return"||k=="KP_Enter"||k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86AudioMute")CloseBanner();};
   bool g1=banner.GrabKeyTopmost(keyReturn),g2=banner.GrabKeyTopmost(keyBack),g3=banner.GrabKeyTopmost(keyMute);
   banner.Show();bannerVisible=true;
   double secs;if(b.Length>4&&double.TryParse(b[4],NumberStyles.Float,CultureInfo.InvariantCulture,out secs)&&secs>=1){bannerTimer=new Tizen.NUI.Timer((uint)(Math.Min(secs,3600)*1000));bannerTimer.Tick+=(o,e)=>{CloseBanner();return false;};bannerTimer.Start();}
   if(!bannerHeld){bannerHeld=true;System.Threading.Interlocked.Increment(ref haBusy);}KeepWarm();
   Tlog("banner shown grab="+g1+","+g2+","+g3+" "+b[0]);
  }catch(Exception x){Tlog("banner fail "+x.GetType().Name+" "+x.Message);banner=null;}
 }
 void CloseBanner(){
  bannerTimer?.Stop();bannerTimer=null;
  try{if(banner!=null){banner.UngrabKeyTopmost(keyReturn);banner.UngrabKeyTopmost(keyBack);banner.UngrabKeyTopmost(keyMute);banner.Hide();banner.Dispose();banner=null;}}catch{banner=null;}
  bannerVisible=false;Tlog("banner closed");
  if(bannerQueue.Count>0){NextBanner();return;}
  if(bannerHeld){bannerHeld=false;haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}
 }
// Icons drawn for the banners (white on the coloured tile), written once to the helper's own data folder.
 static readonly Dictionary<string,string> bannerIcons=new Dictionary<string,string>{
  {"info","<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><circle cx='32' cy='32' r='26' fill='none' stroke='#fff' stroke-width='5'/><rect x='29' y='27' width='6' height='20' rx='3' fill='#fff'/><circle cx='32' cy='19' r='4' fill='#fff'/></svg>"},
  {"pluie","<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><path d='M18 38a11 11 0 0 1 1-22 15 15 0 0 1 28 3 9.5 9.5 0 0 1-1 19z' fill='#fff'/><g stroke='#fff' stroke-width='4' stroke-linecap='round'><path d='M22 45l-3 8'/><path d='M33 45l-3 8'/><path d='M44 45l-3 8'/></g></svg>"},
  {"felix","<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><circle cx='32' cy='32' r='26' fill='none' stroke='#fff' stroke-width='5'/><circle cx='32' cy='22' r='7' fill='#fff'/><path d='M14 38h36' stroke='#fff' stroke-width='4' stroke-linecap='round'/></svg>"},
  {"plex","<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><path d='M22 10h14l16 22-16 22H22l16-22z' fill='#fff'/></svg>"},
  {"maison","<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><path d='M10 30L32 11l22 19' fill='none' stroke='#fff' stroke-width='5' stroke-linecap='round' stroke-linejoin='round'/><path d='M17 27v25h30V27' fill='none' stroke='#fff' stroke-width='5' stroke-linejoin='round'/><rect x='27' y='37' width='10' height='15' fill='#fff'/></svg>"},
  {"velo","<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><g fill='none' stroke='#fff' stroke-width='4' stroke-linecap='round' stroke-linejoin='round'><circle cx='16' cy='42' r='10'/><circle cx='48' cy='42' r='10'/><path d='M16 42l10-18h16l6 18M26 24l8 18h-18M38 16h8'/></g></svg>"},
  {"alerte","<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><path d='M32 8l26 46H6z' fill='none' stroke='#fff' stroke-width='5' stroke-linejoin='round'/><rect x='29' y='24' width='6' height='16' rx='3' fill='#fff'/><circle cx='32' cy='47' r='3.5' fill='#fff'/></svg>"},
 };
 static readonly Dictionary<string,string> iconTint=new Dictionary<string,string>{{"info","#5B7CFA"},{"pluie","#2F80ED"},{"felix","#27AE60"},{"plex","#E5A00D"},{"maison","#8E5CF7"},{"velo","#16A085"},{"alerte","#E74C3C"}};
 // "icon name" → drawn icon; "camera.x" / "image.x" / any entity with a picture → that picture through Home Assistant.
 static string BannerPicture(string what){
  try{
   if(string.IsNullOrEmpty(what))return "";
   if(bannerIcons.TryGetValue(what,out var svg)){string f=System.IO.Path.Combine(SharedDir("icons"),what+".svg");if(!File.Exists(f))File.WriteAllText(f,svg);return f;}
   if(what.StartsWith("/")&&File.Exists(what))return what;
   if(Regex.IsMatch(what,@"^[a-z_]+\.[a-z0-9_]+$")){
    string json=HaGet("/api/states/"+what);if(json==null)return "";
    using(var doc=JsonDocument.Parse(json)){string picture=Str(doc.RootElement.GetProperty("attributes"),"entity_picture");if(picture.StartsWith("/api/"))return HaImage(picture,"banner",Hash(picture+DateTime.UtcNow.Ticks));}
   }
  }catch{}
  return "";
 }
 static string Tint(string icon){return iconTint.TryGetValue(icon??"",out var c)?c:"#5B7CFA";}

 // ---- alerts as banners, over Home, apps or the Apple TV ----
 // Home Assistant pushes them over one long-lived WebSocket: the "belair_notify" event (script « BelAir OS · Message à la TV »)
 // and Félix's state changes arrive at once; rain is checked every 15 minutes.
 Tizen.NUI.Timer watchTimer,pumpTimer;bool watchHeld;static bool pushStarted;
 static readonly System.Collections.Concurrent.ConcurrentQueue<string[]> pendingAlerts=new System.Collections.Concurrent.ConcurrentQueue<string[]>();
 static int checking;static bool rainAlerted;static DateTime lastRainCheck=DateTime.MinValue;
 void StartWatch(){
  if(watchTimer!=null)return;
  if(!watchHeld){watchHeld=true;System.Threading.Interlocked.Increment(ref haBusy);}KeepWarm();
  watchTimer=new Tizen.NUI.Timer(300000);watchTimer.Tick+=(o,e)=>{RunCheck();return true;};watchTimer.Start();
  pumpTimer=new Tizen.NUI.Timer(500);pumpTimer.Tick+=(o,e)=>{if(closeRequested){closeRequested=false;bannerQueue.Clear();if(banner!=null)CloseBanner();}while(pendingAlerts.TryDequeue(out var a))ShowBanner(a[0],a[1],Tint(a[2]),a[2],a.Length>3?a[3]:"0");return true;};pumpTimer.Start();
  RunCheck();StartPush();Tlog("watch on");
 }
 void StopWatch(){
  watchTimer?.Stop();watchTimer=null;pumpTimer?.Stop();pumpTimer=null;
  if(watchHeld){watchHeld=false;haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}
  Tlog("watch off");
 }
 static void StartPush(){
  if(pushStarted)return;pushStarted=true;
  var th=new System.Threading.Thread(()=>{
   while(true){
    try{
     WithHaSocket<string>((ws,ct)=>{
      WsSend(ws,"{\"id\":1,\"type\":\"subscribe_events\",\"event_type\":\"belair_notify\"}",ct);
      WsSend(ws,"{\"id\":3,\"type\":\"subscribe_events\",\"event_type\":\"belair_close\"}",ct);
      WsSend(ws,"{\"id\":4,\"type\":\"subscribe_trigger\",\"trigger\":{\"platform\":\"state\",\"entity_id\":\"media_player.appletv_appletv\"}}",ct);
      WsSend(ws,"{\"id\":2,\"type\":\"subscribe_trigger\",\"trigger\":{\"platform\":\"state\",\"entity_id\":\"vacuum.felix\"}}",ct);
      Tlog("push connected");
      while(ws.State==System.Net.WebSockets.WebSocketState.Open){
       string msg=WsRecv(ws,ct);if(msg==null)break;
       try{using(var doc=JsonDocument.Parse(msg)){
        var r=doc.RootElement;if(Str(r,"type")!="event")continue;var ev=r.GetProperty("event");
        int sid=r.GetProperty("id").GetInt32();
        if(sid==3){closeRequested=true;Tlog("close requested");continue;}
        // The Apple TV remote's centre click toggles play/pause: with a banner up, take it as "close" and put playback back as it was.
        if(sid==4){if(!bannerVisible||DateTime.UtcNow<ignoreAtvUntil)continue;var tr=ev.GetProperty("variables").GetProperty("trigger");string f=Str(tr.GetProperty("from_state"),"state"),t=Str(tr.GetProperty("to_state"),"state");
         if(f=="playing"&&t=="paused"||f=="paused"&&t=="playing"){bannerVisible=false;ignoreAtvUntil=DateTime.UtcNow.AddSeconds(5);closeRequested=true;Tlog("close by Apple TV click "+f+">"+t);string want=f;
          // Plex ignores a command that lands while it is still handling the click: wait, then check once more.
          Task.Run(()=>{for(int attempt=0;attempt<2;attempt++){System.Threading.Thread.Sleep(attempt==0?1300:2200);
           string now="";try{string js=HaGet("/api/states/media_player.appletv_appletv");if(js!=null)using(var d2=JsonDocument.Parse(js))now=Str(d2.RootElement,"state");}catch{}
           if(now==want)break;ignoreAtvUntil=DateTime.UtcNow.AddSeconds(5);
           HaPost("/api/services/media_player/"+(want=="playing"?"media_play":"media_pause"),"{\"entity_id\":\"media_player.appletv_appletv\"}");Tlog("restore "+want+" (was "+now+")");}});}
         continue;}
        if(sid==1){var d=ev.GetProperty("data");string img=Str(d,"image");string to=d.TryGetProperty("timeout",out var tv)?tv.ToString():"0";
         Alert(Str(d,"title")!=""?Str(d,"title"):"Maison",Str(d,"message"),img!=""?img:"maison",to);}
        else{var trig=ev.GetProperty("variables").GetProperty("trigger");string from=Str(trig.GetProperty("from_state"),"state"),now=Str(trig.GetProperty("to_state"),"state");
         if(from=="cleaning"&&(now=="returning"||now=="docked"))Alert("Félix a terminé","Le ménage est fini, il rentre à sa base.","image.felix_map_0");
         else if(now=="error"&&from!="error")Alert("Félix a besoin d’aide","Il est bloqué ou en erreur. Voir la page Félix ou l’appli Roborock.","alerte");}
       }}catch{}
      }
      return null;
     },86400);
    }catch{}
    Tlog("push reconnect");System.Threading.Thread.Sleep(15000);
   }
  }){IsBackground=true};
  th.Start();
 }
 static void RunCheck(){
  if(System.Threading.Interlocked.Exchange(ref checking,1)==1)return;
  Task.Run(()=>{try{CheckRain();}catch{}finally{System.Threading.Interlocked.Exchange(ref checking,0);}});
 }
 static void Alert(string title,string body,string icon,string timeout="0"){pendingAlerts.Enqueue(new[]{title,body,icon,timeout});Tlog("alert "+title);}
 // Rain within the hour (Open-Meteo, 15-minute steps), once per shower.
 static void CheckRain(){
  if((DateTime.UtcNow-lastRainCheck).TotalMinutes<15)return;
  lastRainCheck=DateTime.UtcNow;var rain=FetchRain();
  if(rain==null||rain.Count<5)return;
  if(rain.All(r=>r.Item2<0.1)){rainAlerted=false;return;}
  if(!rainAlerted&&rain[0].Item2<0.1){int i=rain.FindIndex(r=>r.Item2>=0.2);if(i>0&&i<=4){rainAlerted=true;double total=rain.Skip(i).Take(4).Sum(r=>r.Item2);
   Alert("Pluie vers "+rain[i].Item1,"Environ "+total.ToString("0.0",CultureInfo.GetCultureInfo("fr-FR"))+" mm attendus dans l’heure à Paris 12e. Pensez au parapluie.","pluie");}}
 }
 // ---- rain in the next two hours, 15-minute steps (same place as the forecast) ----
 static HttpClient webClient;
 static string WebGet(string url){
  if(webClient==null){var c=new HttpClient{Timeout=TimeSpan.FromSeconds(12)};c.DefaultRequestHeaders.Add("User-Agent","BelAirOS-TV/1.0");webClient=c;}
  try{return webClient.GetStringAsync(url).GetAwaiter().GetResult();}catch{return null;}
 }
 static List<Tuple<string,double>> FetchRain(){
  string json=WebGet("https://api.open-meteo.com/v1/forecast?latitude=48.8566&longitude=2.3522&minutely_15=precipitation&forecast_minutely_15=9&timezone=Europe%2FParis");
  if(json==null)return null;
  try{using(var doc=JsonDocument.Parse(json)){
   var m=doc.RootElement.GetProperty("minutely_15");var times=m.GetProperty("time");var mm=m.GetProperty("precipitation");
   var list=new List<Tuple<string,double>>();
   // the first step is the current quarter of an hour
   for(int i=0;i<times.GetArrayLength();i++){string t=times[i].GetString()??"";list.Add(Tuple.Create(t.Length>=16?t.Substring(11,5):t,mm[i].ValueKind==JsonValueKind.Number?mm[i].GetDouble():0));}
   return list;
  }}catch{return null;}
 }
 static string FetchRainJson(){
  var rain=FetchRain();if(rain==null)return null;
  return JsonSerializer.Serialize(rain.Select(r=>new object[]{r.Item1,Math.Round(r.Item2,2)}).ToList());
 }
 // ---- Vélib' stations and Lime bikes around Bel-Air (public GBFS feeds, no account) ----
 const double belAirLat=48.8566,belAirLon=2.3522;
 static List<string[]> velibNear;static DateTime velibInfoAt=DateTime.MinValue;
 // The plan is centred on home: Home Assistant's zone.home (never stored in the code); Bel-Air metro until it is read.
 static double ctrLat=belAirLat,ctrLon=belAirLon;static bool ctrLoaded;
 // Demo mode for screenshots (/tmp/quiet-demo): plans centred on the Élysée palace, fictional devices on Localiser.
 static bool Demo(){try{return File.Exists("/tmp/quiet-demo");}catch{return false;}}static bool demoCenter;
 static void LoadCenter(){
  bool demo=Demo();
  if(demo!=demoCenter){demoCenter=demo;velibNear=null;ctrLoaded=false;Tlog("centre "+(demo?"demo":"home"));if(demo){ctrLat=48.87042;ctrLon=2.31674;ctrLoaded=true;return;}}
  if(ctrLoaded)return;
  try{string json=HaGet("/api/states/zone.home");if(json==null)return;using(var doc=JsonDocument.Parse(json)){var a=doc.RootElement.GetProperty("attributes");
   if(a.TryGetProperty("latitude",out var la)&&la.ValueKind==JsonValueKind.Number&&a.TryGetProperty("longitude",out var lo)&&lo.ValueKind==JsonValueKind.Number){ctrLat=la.GetDouble();ctrLon=lo.GetDouble();ctrLoaded=true;velibNear=null;}}}catch{}
 }
 static double Dx(double lon){return (lon-ctrLon)*111320*Math.Cos(ctrLat*Math.PI/180);}
 static double Dy(double lat){return (lat-ctrLat)*111320;}
 static double Meters(double lat,double lon){double dx=Dx(lon),dy=Dy(lat);return Math.Sqrt(dx*dx+dy*dy);}
 static string FetchMobilityJson(){
  LoadCenter();
  const string velib="https://velib-metropole-opendata.smovengo.cloud/opendata/Velib_Metropole/";
  if(velibNear==null||(DateTime.UtcNow-velibInfoAt).TotalHours>=24){
   string info=WebGet(velib+"station_information.json");
   if(info!=null)try{using(var doc=JsonDocument.Parse(info)){
    var list=new List<Tuple<double,string,string>>();
    foreach(var st in doc.RootElement.GetProperty("data").GetProperty("stations").EnumerateArray()){double la=st.GetProperty("lat").GetDouble(),lo=st.GetProperty("lon").GetDouble(),d=Meters(la,lo);if(d<700)list.Add(Tuple.Create(d,st.GetProperty("station_id").ToString(),Str(st,"name")+"|"+(int)Math.Round(Dx(lo))+"|"+(int)Math.Round(Dy(la))));}
    velibNear=list.OrderBy(x=>x.Item1).Take(4).Select(x=>{var q=x.Item3.Split('|');return new[]{x.Item2,q[0],((int)x.Item1).ToString(),q[1],q[2]};}).ToList();velibInfoAt=DateTime.UtcNow;
   }}catch{}
  }
  var ms=new MemoryStream();
  using(var w=new Utf8JsonWriter(ms)){
   w.WriteStartObject();w.WriteStartArray("Velib");
   string status=velibNear!=null&&velibNear.Count>0?WebGet(velib+"station_status.json"):null;
   if(status!=null)try{using(var doc=JsonDocument.Parse(status)){
    var byId=new Dictionary<string,JsonElement>();foreach(var st in doc.RootElement.GetProperty("data").GetProperty("stations").EnumerateArray())byId[st.GetProperty("station_id").ToString()]=st.Clone();
    foreach(var n in velibNear){if(!byId.TryGetValue(n[0],out var st))continue;int mech=0,ebike=0;
     if(st.TryGetProperty("num_bikes_available_types",out var types))foreach(var t in types.EnumerateArray()){if(t.TryGetProperty("mechanical",out var m)&&m.ValueKind==JsonValueKind.Number)mech+=m.GetInt32();if(t.TryGetProperty("ebike",out var eb)&&eb.ValueKind==JsonValueKind.Number)ebike+=eb.GetInt32();}
     bool open=!st.TryGetProperty("is_renting",out var rent)||rent.ValueKind!=JsonValueKind.Number||rent.GetInt32()==1;
     w.WriteStartObject();w.WriteString("Name",n[1]);w.WriteNumber("Dist",int.Parse(n[2]));w.WriteNumber("Mech",mech);w.WriteNumber("Ebike",ebike);w.WriteNumber("Docks",st.TryGetProperty("num_docks_available",out var dk)&&dk.ValueKind==JsonValueKind.Number?dk.GetInt32():0);w.WriteBoolean("Open",open);w.WriteNumber("Dx",int.Parse(n[3]));w.WriteNumber("Dy",int.Parse(n[4]));w.WriteEndObject();}
   }}catch{}
   w.WriteEndArray();
   string lime=WebGet("https://data.lime.bike/api/partners/v2/gbfs/paris/free_bike_status");
   int count=0;double nearest=-1;var pts=new List<int[]>();
   if(lime!=null)try{using(var doc=JsonDocument.Parse(lime))foreach(var b in doc.RootElement.GetProperty("data").GetProperty("bikes").EnumerateArray()){
    if(b.TryGetProperty("is_disabled",out var dis)&&dis.ValueKind==JsonValueKind.True||b.TryGetProperty("is_reserved",out var res)&&res.ValueKind==JsonValueKind.True)continue;
    double la=b.GetProperty("lat").GetDouble(),lo=b.GetProperty("lon").GetDouble(),d=Meters(la,lo);if(d<=400){count++;if(nearest<0||d<nearest)nearest=d;if(pts.Count<120){string vt=Str(b,"vehicle_type");int range=b.TryGetProperty("current_range_meters",out var cr)&&cr.ValueKind==JsonValueKind.Number?(int)cr.GetDouble():-1;pts.Add(new[]{(int)Math.Round(Dx(lo)),(int)Math.Round(Dy(la)),range,vt=="e-bike"?0:vt=="scooter"?1:2});}}
   }}catch{count=-1;}
   w.WriteStartObject("Lime");w.WriteNumber("Count",lime==null?-1:count);w.WriteNumber("Near",(int)Math.Round(nearest));w.WriteStartArray("Pts");foreach(var q in pts){w.WriteStartArray();foreach(var v in q)w.WriteNumberValue(v);w.WriteEndArray();}w.WriteEndArray();w.WriteEndObject();
   w.WriteString("At",DateTime.Now.ToString("HH:mm"));w.WriteNumber("MetroDx",(int)Math.Round(Dx(belAirLon)));w.WriteNumber("MetroDy",(int)Math.Round(Dy(belAirLat)));w.WriteBoolean("Home",ctrLoaded);
   w.WriteEndObject();
  }
  return System.Text.Encoding.UTF8.GetString(ms.ToArray());
 }
 // ---- Localiser: every tracked device with a position (iCloud, companion app), relative to home ----
 const string findTemplate="{% set ns=namespace(o=[]) %}{% for s in states.device_tracker if s.attributes.latitude is defined and s.attributes.longitude is defined %}{% set d=device_id(s.entity_id) %}{% set ents=device_entities(d) if d else [] %}{% set snd=ents|select('search','play_sound')|first|default('') %}{% set loc=ents|select('search','last_located')|first|default('') %}{% set ns.o=ns.o+[{'id':s.entity_id,'name':s.name,'lat':s.attributes.latitude,'lon':s.attributes.longitude,'bat':s.attributes.battery|default(-1),'bs':s.attributes.battery_status|default(''),'model':s.attributes.device_name|default(''),'state':s.state,'zone':(state_attr('zone.'~s.state,'friendly_name') or ''),'acc':s.attributes.gps_accuracy|default(0),'sound':snd,'located':(states(loc) if loc else s.last_updated|string)}] %}{% endfor %}{{ ns.o|tojson }}";
 static readonly System.Collections.Concurrent.ConcurrentDictionary<string,double[]> findPos=new System.Collections.Concurrent.ConcurrentDictionary<string,double[]>();
 // Web Mercator "world" coordinates (0..256 at zoom 0): exact positions at any scale, from the street to the whole world.
 static double[] Merc(double lat,double lon){double r=lat*Math.PI/180;return new[]{(lon+180)/360*256,(1-Math.Log(Math.Tan(r)+1/Math.Cos(r))/Math.PI)/2*256};}
 static string FetchFindJson(){
  LoadCenter();
  if(Demo()){
   var now=DateTime.UtcNow;string Iso(int min)=>now.AddMinutes(-min).ToString("yyyy-MM-ddTHH:mm:ss+00:00");
   var demo=new[]{new{n="iPhone",m="iPhone 17 Pro",st="home",b=82,bs="NotCharging",dx=8,dy=-5,acc=12,t=1},new{n="MacBook Air",m="MacBook Air (13-inch, M5)",st="home",b=64,bs="Charging",dx=-14,dy=9,acc=35,t=4},
    new{n="AirPods Pro",m="AirPods Pro (2nd generation)",st="not_home",b=-1,bs="",dx=430,dy=-260,acc=18,t=9},new{n="iPhone (famille)",m="iPhone 17",st="not_home",b=41,bs="NotCharging",dx=-820,dy=610,acc=9,t=2},new{n="iPad",m="iPad Air",st="not_home",b=17,bs="NotCharging",dx=1900,dy=-1300,acc=25,t=26}};
   var dms=new MemoryStream();using(var w=new Utf8JsonWriter(dms)){var hm=Merc(ctrLat,ctrLon);w.WriteStartObject();w.WriteStartArray("home");w.WriteNumberValue(hm[0]);w.WriteNumberValue(hm[1]);w.WriteEndArray();w.WriteStartArray("items");int i=0;foreach(var d in demo){i++;double dla=ctrLat+d.dy/111320.0,dlo=ctrLon+d.dx/(111320.0*Math.Cos(ctrLat*Math.PI/180));var dm=Merc(dla,dlo);w.WriteStartObject();w.WriteNumber("wx",dm[0]);w.WriteNumber("wy",dm[1]);w.WriteString("id","device_tracker.demo_"+i);w.WriteString("name",d.n);w.WriteString("bs",d.bs);w.WriteString("model",d.m);w.WriteString("state",d.st);w.WriteString("zone","");w.WriteString("sound","button.demo_play_sound");w.WriteString("located",Iso(d.t));w.WriteNumber("bat",d.b);w.WriteNumber("acc",d.acc);w.WriteNumber("dist",(long)Math.Sqrt(d.dx*d.dx+d.dy*d.dy));w.WriteNumber("dx",d.dx);w.WriteNumber("dy",d.dy);w.WriteEndObject();
    findPos["device_tracker.demo_"+i]=new[]{dla,dlo};}w.WriteEndArray();w.WriteEndObject();}
   return System.Text.Encoding.UTF8.GetString(dms.ToArray());
  }
  string raw=HaPostJson("/api/template",JsonSerializer.Serialize(new{template=findTemplate}));if(raw==null)return null;
  var ms=new MemoryStream();
  try{using(var doc=JsonDocument.Parse(raw))using(var w=new Utf8JsonWriter(ms)){
   var hm=Merc(ctrLat,ctrLon);w.WriteStartObject();w.WriteStartArray("home");w.WriteNumberValue(hm[0]);w.WriteNumberValue(hm[1]);w.WriteEndArray();
   w.WriteStartArray("items");
   foreach(var d in doc.RootElement.EnumerateArray()){
    double la=d.GetProperty("lat").GetDouble(),lo=d.GetProperty("lon").GetDouble();double dx=Dx(lo),dy=Dy(la);findPos[Str(d,"id")]=new[]{la,lo};
    w.WriteStartObject();var wm=Merc(la,lo);w.WriteNumber("wx",wm[0]);w.WriteNumber("wy",wm[1]);
    foreach(var k in new[]{"id","name","bs","model","state","zone","sound","located"})w.WriteString(k,Str(d,k));
    w.WriteNumber("bat",d.TryGetProperty("bat",out var b)&&b.ValueKind==JsonValueKind.Number?b.GetInt32():-1);
    w.WriteNumber("acc",d.TryGetProperty("acc",out var a)&&a.ValueKind==JsonValueKind.Number?(int)a.GetDouble():0);
    // far away (another city or country): the exact offset is not needed, only the distance
    double dist=Math.Sqrt(dx*dx+dy*dy);
    w.WriteNumber("dist",(long)Math.Round(dist));w.WriteNumber("dx",dist<20000?(int)Math.Round(dx):0);w.WriteNumber("dy",dist<20000?(int)Math.Round(dy):0);
    w.WriteEndObject();
   }
   w.WriteEndArray();w.WriteEndObject();
  }}catch{return null;}
  return System.Text.Encoding.UTF8.GetString(ms.ToArray());
 }
 // ---- street map behind the plans: IGN's Plan IGN (open licence, no key; clean, Apple Plans-like), OpenStreetMap
 // when a tile is missing. Fetched once and kept (a few dozen tiles per view). ----
 // request "dark|width|height|radius": the plan is width×height display pixels, 400 m = radius pixels, centred on Bel-Air.
 static int basemapCalls;
 static readonly System.Threading.SemaphoreSlim tileGate=new System.Threading.SemaphoreSlim(12);
 // One map tile from the cache, else Plan IGN, else OpenStreetMap (IGN has no detailed tiles outside France).
 static string GetTile(string dir,int z,int x,int y){
  string f=System.IO.Path.Combine(dir,z+"_"+x+"_"+y+".png");if(File.Exists(f))return f;
  if(webClient==null)WebGet("about:blank");
  tileGate.Wait();
  try{foreach(var url in new[]{"https://data.geopf.fr/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=GEOGRAPHICALGRIDSYSTEMS.PLANIGNV2&STYLE=normal&TILEMATRIXSET=PM&TILEMATRIX="+z+"&TILEROW="+y+"&TILECOL="+x+"&FORMAT=image/png","https://tile.openstreetmap.org/"+z+"/"+x+"/"+y+".png"}){
   if(File.Exists(f))break;
   try{var bytes=webClient.GetByteArrayAsync(url).GetAwaiter().GetResult();if(bytes.Length>100&&bytes[0]==0x89){File.WriteAllBytes(f+".part",bytes);File.Move(f+".part",f,true);}}catch{}
  }}finally{tileGate.Release();}
  return File.Exists(f)?f:null;
 }
 // Fetches in the background the tiles of the zoom levels just above and below the view, so + and − are instant.
 static void PrefetchAround(string dir,int z,double cx,double cy,double hw,double hh){
  Task.Run(()=>{try{
   foreach(int zz in new[]{z-1,z+1}){if(zz<0||zz>19)continue;double k=Math.Pow(2,zz-z),fx=cx*k,fy=cy*k,w=hw*k*(zz>z?0.6:1),h=hh*k*(zz>z?0.6:1);int n=1<<zz;
    var list=new List<Task>();
    for(int tx=(int)Math.Floor((fx-w)/256);tx<=(int)Math.Floor((fx+w)/256);tx++)for(int ty=(int)Math.Floor((fy-h)/256);ty<=(int)Math.Floor((fy+h)/256);ty++){if(ty<0||ty>=n||list.Count>=60)continue;int x=((tx%n)+n)%n,y=ty;list.Add(Task.Run(()=>GetTile(dir,zz,x,y)));}
    Task.WaitAll(list.ToArray());}
  }catch{}});
 }
 static string FetchBasemapJson(string request){
  var p=request.Split('|');if(p.Length<4)return null;
  foreach(var old in new[]{"basemap-dark_all","basemap-light_all","basemap-osm"})try{string od=System.IO.Path.Combine(SharedDir(""),old);if(Directory.Exists(od))Directory.Delete(od,true);}catch{}
  int W=int.Parse(p[1]),H=int.Parse(p[2]);double R=double.Parse(p[3],CultureInfo.InvariantCulture);int z=16;
  // centre: home, a device, or a point the map was moved to ("pt:x,y" in Web Mercator world units)
  LoadCenter();double cLat=ctrLat,cLon=ctrLon;
  if(p.Length>4&&p[4].StartsWith("device_tracker.")){if(!findPos.ContainsKey(p[4]))FetchFindJson();if(findPos.TryGetValue(p[4],out var fp)){cLat=fp[0];cLon=fp[1];}else return null;}
  else if(p.Length>4&&p[4].StartsWith("pt:")){var q=p[4].Substring(3).Split(',');double wx=double.Parse(q[0],CultureInfo.InvariantCulture),wy=Math.Max(0.5,Math.Min(255.5,double.Parse(q[1],CultureInfo.InvariantCulture)));cLon=wx/256*360-180;cLat=Math.Atan(Math.Sinh(Math.PI*(1-2*wy/256)))*180/Math.PI;}
  // zoom level whose tiles are shown at or below their own size (sharp): never stretched more than 10 %
  double zlat=cLat*Math.PI/180;
  Func<int,double> scale=zz=>R/400.0*156543.03392*Math.Cos(zlat)/Math.Pow(2,zz);
  while(z<19&&scale(z)>1.1)z++;
  while(z>0&&scale(z)<0.55)z--;
  double latr=cLat*Math.PI/180,world=256*Math.Pow(2,z);
  double cx=(cLon+180)/360*world,cy=(1-Math.Log(Math.Tan(latr)+1/Math.Cos(latr))/Math.PI)/2*world;
  double mpp=156543.03392*Math.Cos(latr)/Math.Pow(2,z),sc=R/400.0*mpp;
  double hw=W/2.0/sc,hh=H/2.0/sc;
  int tx0=(int)Math.Floor((cx-hw)/256),tx1=(int)Math.Floor((cx+hw)/256),ty0=(int)Math.Floor((cy-hh)/256),ty1=(int)Math.Floor((cy+hh)/256);
  if((tx1-tx0+1)*(ty1-ty0+1)>96){Tlog("basemap R="+R+" z="+z+" too many tiles "+(tx1-tx0+1)*(ty1-ty0+1));return null;}
  string dir=SharedDir("basemap-ign");
  // keep the tile cache bounded (about 2,000 tiles, ~75 MB): drop the oldest quarter when it grows past that
  if(++basemapCalls%40==1)Task.Run(()=>{try{var files=new System.IO.DirectoryInfo(dir).GetFiles("*.png");if(files.Length>2000)foreach(var f in files.OrderBy(f=>f.LastWriteTimeUtc).Take(files.Length-1500))f.Delete();}catch{}});
  var jobs=new List<Task<string[]>>();
  int nTiles=1<<z;
  for(int tx=tx0;tx<=tx1;tx++)for(int ty=ty0;ty<=ty1;ty++){if(ty<0||ty>=nTiles)continue;int x=((tx%nTiles)+nTiles)%nTiles,y=ty,place=tx;
   jobs.Add(Task.Run(()=>{string f=GetTile(dir,z,x,y);return f!=null?new[]{f,((int)Math.Round((place*256.0-cx)*sc)).ToString(),((int)Math.Round((y*256.0-cy)*sc)).ToString()}:null;}));}
  var tw=Stopwatch.StartNew();Task.WaitAll(jobs.ToArray());PrefetchAround(dir,z,cx,cy,hw,hh);
  Tlog("basemap R="+R+" z="+z+" tiles="+jobs.Count+" ok="+jobs.Count(j=>j.Result!=null)+" "+tw.ElapsedMilliseconds+"ms");
  var ms=new MemoryStream();
  using(var w=new Utf8JsonWriter(ms)){w.WriteStartObject();w.WriteString("Key",request+(ctrLoaded||p.Length>4?"":"|metro"));w.WriteNumber("Size",(int)Math.Ceiling(256*sc));w.WriteNumber("Missing",jobs.Count(j=>j.Result==null));var cw=Merc(cLat,cLon);w.WriteNumber("CWx",cw[0]);w.WriteNumber("CWy",cw[1]);w.WriteStartArray("Tiles");
   foreach(var j in jobs){var t=j.Result;if(t==null)continue;w.WriteStartArray();w.WriteStringValue(t[0]);w.WriteNumberValue(int.Parse(t[1]));w.WriteNumberValue(int.Parse(t[2]));w.WriteEndArray();}
   w.WriteEndArray();w.WriteEndObject();}
  return System.Text.Encoding.UTF8.GetString(ms.ToArray());
 }
 static void ToastProbe(string text,string mode){
  try{
   if(mode=="status"){Tlog("toast status rc="+notification_status_message_post(text));return;}
   IntPtr n=notification_create(0);if(n==IntPtr.Zero){Tlog("toast create null");return;}
   int a=notification_set_text(n,0,"BelAir OS",null,-1),b=notification_set_text(n,1,text,null,-1);
   int c=notification_set_display_applist(n,mode=="active"?0x1f:0x0f);
   int r=notification_post(n);notification_free(n);
   Tlog("toast "+mode+" set="+a+","+b+","+c+" post="+r);
  }catch(Exception x){Tlog("toast fail "+x.GetType().Name+" "+x.Message);}
 }
 protected override void OnCreate(){base.OnCreate();Window.Default.Hide();try{sharedRoot=DirectoryInfo.SharedTrusted;}catch{}
  // Open the Home Assistant connection now: the first TLS handshake and JIT otherwise land on the first button press.
  Task.Run(()=>{try{var sw=Stopwatch.StartNew();HaStatesJson("");Tlog("warmup "+sw.ElapsedMilliseconds+"ms");}catch{}});}
 static readonly DateTime procStart=Process.GetCurrentProcess().StartTime;
 static void Tlog(string m){try{var f="/tmp/quiet-ha-timing.txt";if(File.Exists(f)&&new FileInfo(f).Length>40000)File.Delete(f);File.AppendAllText(f,DateTime.Now.ToString("HH:mm:ss.fff")+" "+m+"\n");}catch{}}
 protected override void OnAppControlReceived(AppControlReceivedEventArgs e){
  base.OnAppControlReceived(e);Tlog("recv up="+(int)(DateTime.Now-procStart).TotalMilliseconds+"ms");
  // Commands that act on the home (Home Assistant services, Apple TV, Plex, banners) are accepted from BelAir's Home only.
  string caller="";try{caller=e.ReceivedAppControl.CallerApplicationId??"";}catch{}
  if(caller!="com.samsung.tv.bixby-promotion"){
   foreach(var key in new[]{"quiet_ha_calls","quiet_ha_calls_silent","quiet_ha_domain","quiet_ha_batch","quiet_channel","quiet_plex_play","quiet_govee_scene","quiet_toast"}){
    string v;if(e.ReceivedAppControl.ExtraData.TryGet(key,out v)){Tlog("refused "+key+" from "+(caller==""?"?":caller));if(idleTimer==null&&!watchHeld)Exit();return;}
   }
  }string kind;bool preload=e.ReceivedAppControl.ExtraData.TryGet("launch_type",out kind)&&kind=="preload";
  string toast;if(e.ReceivedAppControl.ExtraData.TryGet("quiet_toast",out toast)&&!string.IsNullOrEmpty(toast)){string mode;if(!e.ReceivedAppControl.ExtraData.TryGet("quiet_toast_mode",out mode))mode="active";if(mode=="window"){string tt;if(!e.ReceivedAppControl.ExtraData.TryGet("quiet_toast_title",out tt))tt="BelAir OS";ShowBanner(tt,toast);return;}ToastProbe(toast,mode);if(idleTimer==null)Exit();return;}
  string watch;if(e.ReceivedAppControl.ExtraData.TryGet("quiet_watch",out watch)){if(watch=="1")StartWatch();else StopWatch();if(idleTimer==null&&!watchHeld)Exit();return;}
  string articleUrl;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_article_request",out articleUrl)&&!string.IsNullOrEmpty(articleUrl)){
   haLast=DateTime.UtcNow;System.Threading.Interlocked.Increment(ref haBusy);string au=articleUrl;
   Task.Run(()=>{try{ReplyArticle(FetchArticleJson(au));}catch{}finally{haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}});
   KeepWarm();return;
  }
  string plexPlay;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_plex_play",out plexPlay)&&!string.IsNullOrEmpty(plexPlay)){
   haLast=DateTime.UtcNow;System.Threading.Interlocked.Increment(ref haBusy);string pp=plexPlay;
   Task.Run(()=>{try{PlayOnPlex(pp);}catch{}finally{haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}});
   KeepWarm();return;
  }
  string mediaRequest,felixRequest,seriesRequest;
  bool wantSeries=e.ReceivedAppControl.ExtraData.TryGet("quiet_series_request",out seriesRequest)&&!string.IsNullOrEmpty(seriesRequest);
  string factsRequest;bool wantFacts=e.ReceivedAppControl.ExtraData.TryGet("quiet_facts_request",out factsRequest)&&factsRequest=="1";
  bool wantMedia=e.ReceivedAppControl.ExtraData.TryGet("quiet_media_request",out mediaRequest)&&!string.IsNullOrEmpty(mediaRequest);
  bool wantFelix=e.ReceivedAppControl.ExtraData.TryGet("quiet_felix_request",out felixRequest)&&(felixRequest=="1"||felixRequest=="2");
  string mobRequest;bool wantMob=e.ReceivedAppControl.ExtraData.TryGet("quiet_mobility_request",out mobRequest)&&mobRequest=="1";
  string findRequest;bool wantFind=e.ReceivedAppControl.ExtraData.TryGet("quiet_find_request",out findRequest)&&findRequest=="1";
  string mapRequest;bool wantMap=e.ReceivedAppControl.ExtraData.TryGet("quiet_basemap_request",out mapRequest)&&!string.IsNullOrEmpty(mapRequest);
  if(wantMedia||wantFelix||wantSeries||wantFacts||wantMob||wantMap||wantFind){
   haLast=DateTime.UtcNow;System.Threading.Interlocked.Increment(ref haBusy);bool hubs=mediaRequest=="all",wm=wantMedia,wf=wantFelix,live=felixRequest=="2";string sr=wantSeries?seriesRequest:null;bool wfa=wantFacts,wmo=wantMob,wfi=wantFind;string mq=wantMap?mapRequest:null;
   Task.Run(()=>{try{
    var mobTask=wmo?Task.Run(FetchMobilityJson):Task.FromResult<string>(null);
    var rainTask=wmo?Task.Run(FetchRainJson):Task.FromResult<string>(null);
    var findTask=wfi?Task.Run(FetchFindJson):Task.FromResult<string>(null);
    var mapTask=mq!=null?Task.Run(()=>FetchBasemapJson(mq)):Task.FromResult<string>(null);
    var mediaTask=wm?Task.Run(()=>FetchMediaJson(hubs)):Task.FromResult<string>(null);
    var felixTask=wf?Task.Run(()=>FetchFelixJson(live)):Task.FromResult<string>(null);
    var seriesTask=sr!=null?Task.Run(()=>FetchSeriesJson(sr)):Task.FromResult<string>(null);
    var factsTask=wfa?Task.Run(FetchFactsJson):Task.FromResult<string>(null);
    Task.WaitAll(mediaTask,felixTask,seriesTask,factsTask,mobTask,rainTask,mapTask,findTask);
    ReplyExtra(mediaTask.Result,felixTask.Result,seriesTask.Result,factsTask.Result,mobTask.Result,rainTask.Result,mapTask.Result,findTask.Result);
   }catch{}finally{haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}});
   KeepWarm();return;
  }
  string goveeScene;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_govee_scene",out goveeScene)&&!string.IsNullOrEmpty(goveeScene)){
   haLast=DateTime.UtcNow;System.Threading.Interlocked.Increment(ref haBusy);string gs=goveeScene;
   Task.Run(()=>{try{SendGoveeScene(gs);}catch{}finally{haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}});
   KeepWarm();return;
  }
  string channel;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_channel",out channel)&&channel=="france-2"){
   haLast=DateTime.UtcNow;System.Threading.Interlocked.Increment(ref haBusy);string ch=channel;
   Task.Run(()=>{try{StartChannel(ch);}catch{}finally{haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}});
   KeepWarm();return;
  }
  string haCalls;bool silentCalls=false;
  if(!e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_calls",out haCalls)||string.IsNullOrEmpty(haCalls))silentCalls=e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_calls_silent",out haCalls)&&!string.IsNullOrEmpty(haCalls);
  if(!string.IsNullOrEmpty(haCalls)){
   // Silent calls (Apple TV channel start) never reply: a reply would pull Home back over the HDMI input.
   haLast=DateTime.UtcNow;System.Threading.Interlocked.Increment(ref haBusy);string hc=haCalls;bool quiet=silentCalls;
   Task.Run(()=>{try{bool ok=HaCalls(hc);if(!quiet)ReplyHa(null,ok?"ok":"fail");}catch{}finally{haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}});
   KeepWarm();return;
  }
  string haStates,haDomain,haService,haEntity;
  bool wantStates=e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_states_request",out haStates)&&haStates=="1";
  bool wantAction=e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_domain",out haDomain)&e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_service",out haService)&e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_entity",out haEntity);
  string haPos; bool hasPos=e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_position",out haPos)&&haPos!=null&&haPos.Length>0;
  string haBatch; bool wantBatch=e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_batch",out haBatch)&&haBatch!=null&&haBatch.Length>0;
  string haSens;e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_sensors",out haSens);
  if(wantStates||wantAction||wantBatch){
   // Stay warm between Home Assistant requests: a cold .NET start per command froze the UI for seconds
   // and two back-to-back launches could lose a command while the first instance was exiting.
   haLast=DateTime.UtcNow;System.Threading.Interlocked.Increment(ref haBusy);
   string d=haDomain,sv=haService,en=haEntity,ps=hasPos?haPos:null,bt=wantBatch?haBatch:null,sn=haSens;bool ws=wantStates,wa=wantAction;
   // No app_launcher here: from the app sandbox it only timed out (focus=-1, up to 3 s); the reply app-control resumes Home.
   // Commands keep their own queue (in order) so they never wait behind a state read.
   Task.Run(()=>{try{
    bool ok=true;
    if(wa||bt!=null)lock(actLock){
     if(wa&&!HaActionAllowed(d,sv,en)){ok=false;Tlog("refused "+d+"."+sv);}
     else if(wa){
      string body=ps!=null?("{\"entity_id\":\""+en+"\",\"position\":"+ps+"}"):("{\"entity_id\":\""+en+"\"}");
      ok=HaPost("/api/services/"+d+"/"+sv,LocalLight(d+"/"+sv,body));
     }
     if(bt!=null)ok=HaBatch(bt)&&ok;
    }
    string data=null;var sw=Stopwatch.StartNew();
    if(ws)lock(haLock)data=HaStatesJson(sn);
    if(ws)Tlog("states "+sw.ElapsedMilliseconds+"ms bytes="+(data==null?0:data.Length));
    ReplyHa(data,(wa||bt!=null)?(ok?"ok":"fail"):null);
   }catch{}finally{haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}});
   KeepWarm();return;
  }
  string weatherRequest,metroRequest,trafficRequest,newsRequest;
  bool fetchWeather=e.ReceivedAppControl.ExtraData.TryGet("quiet_weather_request",out weatherRequest)&&weatherRequest=="1";
  bool fetchMetro=e.ReceivedAppControl.ExtraData.TryGet("quiet_metro_request",out metroRequest)&&metroRequest=="1";
  bool fetchTraffic=e.ReceivedAppControl.ExtraData.TryGet("quiet_traffic_request",out trafficRequest)&&trafficRequest=="1";
  bool fetchNews=e.ReceivedAppControl.ExtraData.TryGet("quiet_news_request",out newsRequest)&&newsRequest=="1";
  string linesRequest;bool fetchLines=e.ReceivedAppControl.ExtraData.TryGet("quiet_lines_request",out linesRequest)&&linesRequest=="1";
  string marketsRequest;bool fetchMarkets=e.ReceivedAppControl.ExtraData.TryGet("quiet_markets_request",out marketsRequest)&&marketsRequest=="1";
  string energyRequest;bool fetchEnergy=e.ReceivedAppControl.ExtraData.TryGet("quiet_energy_request",out energyRequest)&&energyRequest=="1";
  if(fetchWeather||fetchMetro||fetchTraffic||fetchNews||fetchLines||fetchMarkets||fetchEnergy){
   // Same warm, off-main-thread path as Home Assistant: one request never blocks or kills another.
   haLast=DateTime.UtcNow;System.Threading.Interlocked.Increment(ref haBusy);
   bool fw=fetchWeather,fm=fetchMetro,ft=fetchTraffic,fn=fetchNews,fl=fetchLines,fk=fetchMarkets,fe=fetchEnergy;
   Task.Run(()=>{try{
    var weatherTask=fw?Task.Run(FetchWeather):Task.FromResult<WeatherSummary>(null);
    var metroTask=fm?Task.Run(FetchMetro):Task.FromResult<MetroSummary>(null);
    var alertsTask=(ft||fl)?Task.Run(FetchAlertsJson):Task.FromResult<string>(null); // traffic and line status share one download
    var newsTask=fn?Task.Run(FetchNews):Task.FromResult<NewsSummary>(null);
    var marketsTask=fk?Task.Run(FetchMarkets):Task.FromResult<List<MarketQuote>>(null);
    var energyTask=fe?Task.Run(FetchEnergyJson):Task.FromResult<string>(null);
    Task.WaitAll(weatherTask,metroTask,alertsTask,newsTask,marketsTask,energyTask);
    var traffic=ft?ParseTraffic(alertsTask.Result):null;
    int response=LaunchHome(weatherTask.Result,metroTask.Result,traffic,newsTask.Result,fl?ParseLines(alertsTask.Result):null,marketsTask.Result!=null&&marketsTask.Result.Count>0?JsonSerializer.Serialize(marketsTask.Result,new JsonSerializerOptions{IncludeFields=true}):null,energyTask.Result);
    try{File.WriteAllText("/tmp/quiet-home-bridge.txt",DateTime.UtcNow.ToString("o")+"\nweather_requested="+fw+"\nmetro_requested="+fm+"\ntraffic_requested="+ft+"\nnews_requested="+fn+"\ntraffic_alert="+(traffic!=null&&traffic.HasAlert)+"\nresult="+response+"\n");}catch{}
   }catch{}finally{haLast=DateTime.UtcNow;System.Threading.Interlocked.Decrement(ref haBusy);}});
   KeepWarm();return;
  }
  int result=0;
  if(!preload){IntPtr c;result=app_control_create(out c);if(result==0){try{
   result=app_control_set_app_id(c,"com.samsung.tv.bixby-promotion");
   if(result==0)result=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(result==0)result=app_control_add_extra_data(c,"quiet_page","settings");
   if(result==0)result=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}}
  try{File.WriteAllText("/tmp/quiet-home-bridge.txt",DateTime.UtcNow.ToString("o")+"\npreload="+preload+"\nresult="+result+"\n");}catch{}
  if(idleTimer==null)Exit(); // when warm, the idle timer ends the process once in-flight work is done
 }
 void KeepWarm(){if(idleTimer!=null)return;idleTimer=new Tizen.NUI.Timer(15000);idleTimer.Tick+=(o,a)=>{if(haBusy==0&&(DateTime.UtcNow-haLast).TotalSeconds>=600){Exit();return false;}return true;};idleTimer.Start();}
 static string[] haUrls; static string haToken;
 static readonly object haLock=new object(),actLock=new object(),clientLock=new object(); static DateTime haLast=DateTime.UtcNow; static int haBusy; static HttpClient haClient; Tizen.NUI.Timer idleTimer;
 static void LoadHaConfig(){
  if(haUrls!=null)return;
  try{using(var doc=JsonDocument.Parse(File.ReadAllText("/opt/usr/apps/com.samsung.tv.quicklaunch/res/ha-config.json"))){
   var r=doc.RootElement;var list=new List<string>();
   if(r.TryGetProperty("base_url",out var b))list.Add(b.GetString().TrimEnd('/'));
   if(r.TryGetProperty("base_url_fallback",out var f))list.Add(f.GetString().TrimEnd('/'));
   haUrls=list.ToArray();haToken=r.GetProperty("token").GetString();
  }}catch{haUrls=new string[0];haToken="";}
 }
 static HttpClient HaClient(){
  lock(clientLock){
  if(haClient!=null)return haClient;
  var h=new HttpClientHandler();
  // Only the local HA (private address, self-signed certificate) may skip validation; the remote URL must present a valid certificate.
  h.ServerCertificateCustomValidationCallback=(m,c,ch,er)=>er==System.Net.Security.SslPolicyErrors.None||m.RequestUri!=null&&IsPrivateHost(m.RequestUri.Host);
  var cl=new HttpClient(h);cl.Timeout=TimeSpan.FromSeconds(15);
  cl.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",haToken);
  haClient=cl;return cl;
  }
 }
 static string HaGet(string path){
  LoadHaConfig();
  foreach(var b in haUrls){try{{var cl=HaClient();var resp=cl.GetAsync(b+path).GetAwaiter().GetResult();if(resp.IsSuccessStatusCode)return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();}}catch{}}
  return null;
 }
 // Govee lights exist twice in Home Assistant: the cloud entity (scenes, but 4–7 s per command)
 // and the LAN entity (~20 ms). Power, brightness and colours use the LAN one; only scenes go to the cloud.
 static readonly Dictionary<string,string> lanLight=new Dictionary<string,string>{{"light.salon_neon_rope_light","light.salon_neon"},{"light.bureau_lantern_floor_lamp","light.h1630_2"},{"light.uplighter_floor_lamp","light.h60b0"},{"light.tv_backlight_3_lite","light.h6099"}};
 static string LocalLight(string path,string body){
  if(!path.StartsWith("light/")||body.Contains("\"effect\""))return body;
  foreach(var kv in lanLight)body=body.Replace("\""+kv.Key+"\"","\""+kv.Value+"\"");
  return body;
 }
 static bool HaPost(string path,string body){
  LoadHaConfig();
  foreach(var b in haUrls){var sw=Stopwatch.StartNew();try{{var cl=HaClient();var ct=new StringContent(body,System.Text.Encoding.UTF8,"application/json");var resp=cl.PostAsync(b+path,ct).GetAwaiter().GetResult();Tlog("post "+path+" "+(int)resp.StatusCode+" "+sw.ElapsedMilliseconds+"ms");if(resp.IsSuccessStatusCode)return true;}}catch(Exception x){Tlog("post "+path+" fail "+sw.ElapsedMilliseconds+"ms "+x.GetType().Name);if(x is TaskCanceledException)return false;}}
  return false;
 }
 static string HaPostJson(string path,string body){
  LoadHaConfig();
  foreach(var b in haUrls){try{var ct=new StringContent(body,System.Text.Encoding.UTF8,"application/json");using(var resp=HaClient().PostAsync(b+path,ct).GetAwaiter().GetResult()){if(resp.IsSuccessStatusCode)return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();}}catch{}}
  return null;
 }
 // Service calls with data, in order, each after an optional wait "w" in ms: [{"p":"select/select_option","b":{...},"w":0},...].
 // Limited to the robot's services and to opening a link / pressing keys on the Apple TV.
 static bool HaCallAllowed(string path,JsonElement body){
  string entity=Str(body,"entity_id");
  if(path=="select/select_option")return entity=="select.felix_mode_de_nettoyage"||entity.StartsWith("select.")&&entity.EndsWith("_music_mode");
  if(path=="switch/turn_on"||path=="switch/turn_off")return entity.StartsWith("switch.")&&new[]{"_music_mode","_dreamview","_bottom_light","_side_light","_ripple_light"}.Any(x=>entity.EndsWith(x));
  if(path.StartsWith("vacuum/"))return entity=="vacuum.felix";
  if(path=="media_player/play_media"||path=="media_player/turn_on"||path=="media_player/select_source")return entity=="media_player.appletv_appletv";
  if(path=="remote/send_command")return entity=="remote.appletv_appletv";
  if(path=="light/turn_on"||path=="light/turn_off")return entity.StartsWith("light.");
  if(path=="button/press")return entity.StartsWith("button.")&&entity.EndsWith("_play_sound");
  return false;
 }
 static bool HaCalls(string json){
  bool all=true;
  try{using(var doc=JsonDocument.Parse(json)){int n=0;foreach(var call in doc.RootElement.EnumerateArray()){
   string path=Str(call,"p");var body=call.GetProperty("b");
   if(!HaCallAllowed(path,body)){all=false;continue;}
   int wait=call.TryGetProperty("w",out var w)&&w.ValueKind==JsonValueKind.Number?w.GetInt32():(n>0?1500:0);n++;
   if(wait>0)System.Threading.Thread.Sleep(Math.Min(wait,15000));
   // Lights go straight out (local ones answer in ~20 ms; a cloud scene can take 7 s and must not hold up the rest).
   bool ok;string raw=LocalLight(path,body.GetRawText());
   if(path.StartsWith("light/"))ok=HaPost("/api/services/"+path,raw);else lock(actLock)ok=HaPost("/api/services/"+path,raw);
   if(!ok)all=false;
  }}}catch{all=false;}
  return all;
 }
 // Live channel on the Apple TV: leave any player, open the CANAL+ channel page, then Down and OK on its live entry.
 // With CANAL+ already open the page is ready in under 2 s; otherwise the app has to start first. No reply (it would pull Home over HDMI).
 static void StartChannel(string slug){
  const string atv="media_player.appletv_appletv",rmt="remote.appletv_appletv";
  string state="",app="";
  try{string json=HaGet("/api/states/"+atv);if(json!=null)using(var doc=JsonDocument.Parse(json)){state=Str(doc.RootElement,"state");app=Str(doc.RootElement.GetProperty("attributes"),"app_name");}}catch{}
  bool asleep=state==""||state=="off"||state=="standby"||state=="unavailable";bool warm=!asleep&&app=="CANAL+";
  Action<string> key=k=>HaPost("/api/services/remote/send_command","{\"entity_id\":\""+rmt+"\",\"command\":\""+k+"\"}");
  if(asleep){HaPost("/api/services/media_player/turn_on","{\"entity_id\":\""+atv+"\"}");System.Threading.Thread.Sleep(1500);}
  key("menu");System.Threading.Thread.Sleep(500);
  HaPost("/api/services/media_player/play_media","{\"entity_id\":\""+atv+"\",\"media_content_type\":\"url\",\"media_content_id\":\"https://www.canalplus.com/chaines/"+slug+"/\"}");
  System.Threading.Thread.Sleep(warm?2200:6500);
  key("down");System.Threading.Thread.Sleep(500);key("select");
  try{File.WriteAllText("/tmp/quiet-channel.txt",DateTime.Now.ToString("HH:mm:ss")+" "+slug+" warm="+warm+" from="+state+"/"+app+"\n");}catch{}
 }
 // ---- Govee scenes over the local network: the light's BLE "scene" packet 33 05 04 [code] wrapped in a LAN ptReal command (UDP 4003) ----
 static readonly HashSet<string> goveeSkus=new HashSet<string>{"H61D3","H1630"};
 static readonly Dictionary<string,string> goveeIp=new Dictionary<string,string>();static DateTime goveeScanAt=DateTime.MinValue;
 // Govee's own LAN discovery (multicast "scan", answers on UDP 4002) keeps the address right if the router hands out a new one.
 static void GoveeScan(){
  goveeScanAt=DateTime.UtcNow;
  try{using(var rx=new System.Net.Sockets.UdpClient()){
   rx.Client.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,System.Net.Sockets.SocketOptionName.ReuseAddress,true);
   rx.Client.Bind(new IPEndPoint(IPAddress.Any,4002));rx.Client.ReceiveTimeout=600;
   using(var tx=new System.Net.Sockets.UdpClient()){var m=System.Text.Encoding.UTF8.GetBytes("{\"msg\":{\"cmd\":\"scan\",\"data\":{\"account_topic\":\"reserve\"}}}");tx.Send(m,m.Length,new IPEndPoint(IPAddress.Parse("239.255.255.250"),4001));}
   var end=DateTime.UtcNow.AddSeconds(2);
   while(DateTime.UtcNow<end){try{var from=new IPEndPoint(IPAddress.Any,0);var d=rx.Receive(ref from);using(var doc=JsonDocument.Parse(d)){var data=doc.RootElement.GetProperty("msg").GetProperty("data");string sku=Str(data,"sku"),ip=Str(data,"ip");if(goveeSkus.Contains(sku)&&ip!="")goveeIp[sku]=ip;}}catch{}}
  }}catch{}
 }
 static void SendGoveeScene(string request){
  var p=request.Split('|');int code;if(p.Length<3||!goveeSkus.Contains(p[0])||!int.TryParse(p[2],out code)||code<=0||code>65535)return;
  byte[] param=new byte[0];if(p.Length>3&&p[3]!="")try{param=Convert.FromBase64String(p[3]);}catch{return;}
  if(param.Length>4000)return;
  if(!goveeIp.ContainsKey(p[0])&&(DateTime.UtcNow-goveeScanAt).TotalMinutes>=5)GoveeScan();
  string ip=goveeIp.TryGetValue(p[0],out var found)?found:p[1];IPAddress addr;if(!IPAddress.TryParse(ip,out addr))return;
  Func<byte[],string> packet=head=>{var b=new byte[20];Array.Copy(head,b,head.Length);byte x=0;for(int i=0;i<19;i++)x^=b[i];b[19]=x;return Convert.ToBase64String(b);};
  var frames=new List<string>{packet(new byte[]{0x33,0x01,0x01})};
  // Animation data first, as govee-local-api / govee2mqtt do: header a3 00 01 <count> 02, then 19-byte frames a3 <index>, the last indexed ff.
  if(param.Length>0){
   var stream=new List<byte>{0xA3,0x00,0x01,0x00,0x02};int count=0,lastIndex=1;
   foreach(var b in param){if(stream.Count%19==0){count++;stream.Add(0xA3);lastIndex=stream.Count;stream.Add((byte)count);}stream.Add(b);}
   stream[lastIndex]=0xFF;stream[3]=(byte)(count+1);
   for(int o=0;o<stream.Count;o+=19)frames.Add(packet(stream.GetRange(o,Math.Min(19,stream.Count-o)).ToArray()));
  }
  frames.Add(packet(new byte[]{0x33,0x05,0x04,(byte)(code&0xFF),(byte)((code>>8)&0xFF)}));
  var msg=System.Text.Encoding.UTF8.GetBytes("{\"msg\":{\"cmd\":\"ptReal\",\"data\":{\"command\":[\""+string.Join("\",\"",frames)+"\"]}}}");
  // The neon's Wi-Fi drops packets: send three times over about a second (a repeated scene just restarts it).
  using(var u=new System.Net.Sockets.UdpClient()){foreach(int wait in new[]{0,400,600}){System.Threading.Thread.Sleep(wait);u.Send(msg,msg.Length,new IPEndPoint(addr,4003));}}
  try{File.WriteAllText("/tmp/quiet-govee.txt",DateTime.Now.ToString("HH:mm:ss")+" "+p[0]+" "+ip+" "+code+" frames="+frames.Count+(goveeIp.ContainsKey(p[0])?" (scan)":" (fallback)")+"\n");}catch{}
 }
 // Services the Home screen uses; anything else is refused even from Home.
 static bool HaActionAllowed(string domain,string service,string entity){
  if(string.IsNullOrEmpty(entity)||!entity.StartsWith(domain+"."))return false;
  switch(domain){
   case "light":case "switch":return service=="turn_on"||service=="turn_off"||service=="toggle";
   case "cover":return service=="set_cover_position"||service=="open_cover"||service=="close_cover"||service=="stop_cover";
   case "scene":return service=="turn_on";
   case "vacuum":return service=="start"||service=="pause"||service=="return_to_base"||service=="stop"||service=="locate";
   case "automation":return service=="turn_on"||service=="turn_off";
   case "button":return service=="press";
   default:return false;
  }
 }
 static bool HaBatch(string json){
  bool all=true;
  try{using(var doc=JsonDocument.Parse(json)){foreach(var a in doc.RootElement.EnumerateArray()){
   string d=a.GetProperty("d").GetString(),s=a.GetProperty("s").GetString(),ent=a.GetProperty("e").GetString();
   if(d==null||s==null||ent==null)continue;
   if(!ent.Split(',').All(x=>HaActionAllowed(d,s,x))){all=false;continue;}
   string eid;
   if(ent.IndexOf(',')>=0){var parts=ent.Split(',');eid="[";for(int i=0;i<parts.Length;i++)eid+=(i>0?",":"")+"\""+parts[i]+"\"";eid+="]";}
   else eid="\""+ent+"\"";
   if(!HaPost("/api/services/"+d+"/"+s,"{\"entity_id\":"+eid+"}"))all=false;
  }}}catch{all=false;}
  return all;
 }
 static bool SensorWanted(string id,string[] patterns){
  foreach(var p in patterns){int star=p.IndexOf('*');
   if(star<0){if(id==p)return true;}
   else if(id.Length>=p.Length-1&&id.StartsWith(p.Substring(0,star))&&id.EndsWith(p.Substring(star+1)))return true;}
  return false;
 }
 static string HaStatesJson(string sensors){
  string json=HaGet("/api/states");if(json==null)return null;
  var patterns=string.IsNullOrEmpty(sensors)?new string[0]:sensors.Split(',');
  var ms=new MemoryStream();
  try{using(var w=new Utf8JsonWriter(ms)){w.WriteStartArray();using(var doc=JsonDocument.Parse(json)){
   var lan=new Dictionary<string,(string st,int bri)>();
   foreach(var s in doc.RootElement.EnumerateArray()){string lid=Str(s,"entity_id");if(!lanLight.ContainsValue(lid))continue;string lst=Str(s,"state");if(lst!="on"&&lst!="off")continue;
    int lb=-1;if(s.TryGetProperty("attributes",out var lat)&&lat.TryGetProperty("brightness",out var lbv)&&lbv.ValueKind==JsonValueKind.Number)lb=lbv.GetInt32();lan[lid]=(lst,lb);}
   foreach(var s in doc.RootElement.EnumerateArray()){
   string id=s.GetProperty("entity_id").GetString();if(id==null)continue;
   if(id.StartsWith("select.")&&id.EndsWith("_music_mode")){w.WriteStartObject();w.WriteString("entity",id);w.WriteString("state",Str(s,"state"));w.WriteNumber("pos",-1);w.WriteStartArray("options");if(s.TryGetProperty("attributes",out var sa)&&sa.TryGetProperty("options",out var so)&&so.ValueKind==JsonValueKind.Array)foreach(var o in so.EnumerateArray())w.WriteStringValue(o.GetString()??"");w.WriteEndArray();w.WriteEndObject();continue;}
   if(!(id.StartsWith("cover.")||id.StartsWith("light.")||id.StartsWith("switch.")||SensorWanted(id,patterns)))continue;
   if(id.StartsWith("light.")&&id.Contains("_segment_"))continue;
   string state=s.TryGetProperty("state",out var st)?st.GetString():"";
   int pos=-1;
   if(id.StartsWith("cover.")&&s.TryGetProperty("attributes",out var at)&&at.TryGetProperty("current_position",out var cp)&&cp.ValueKind==JsonValueKind.Number)pos=cp.GetInt32();
   // Colour lights also report brightness, current scene and the scenes they offer (for the Govee-style panel).
   if(id.StartsWith("light.")&&s.TryGetProperty("attributes",out var la)&&la.TryGetProperty("supported_color_modes",out var cm)&&cm.ValueKind==JsonValueKind.Array&&cm.EnumerateArray().Any(m=>m.GetString()!="onoff"&&m.GetString()!="brightness")){
    int bri=la.TryGetProperty("brightness",out var b)&&b.ValueKind==JsonValueKind.Number?b.GetInt32():-1;
    if(lanLight.TryGetValue(id,out var lid2)&&lan.TryGetValue(lid2,out var lv)){state=lv.st;if(lv.bri>=0)bri=lv.bri;}
    var effects=la.TryGetProperty("effect_list",out var el)&&el.ValueKind==JsonValueKind.Array?el.EnumerateArray().Select(x=>x.GetString()??"").Where(x=>x!=""&&x!="none").ToArray():new string[0];
    w.WriteStartObject();w.WriteString("entity",id);w.WriteString("state",state);w.WriteNumber("pos",pos);w.WriteNumber("bri",bri);w.WriteString("effect",Str(la,"effect"));
    w.WriteStartArray("effects");foreach(var x in effects)w.WriteStringValue(x);w.WriteEndArray();w.WriteBoolean("color",true);w.WriteEndObject();continue;
   }
   w.WriteStartObject();w.WriteString("entity",id);w.WriteString("state",state);w.WriteNumber("pos",pos);w.WriteEndObject();
  }}w.WriteEndArray();}}catch{return null;}
  return System.Text.Encoding.UTF8.GetString(ms.ToArray());
 }
 static int ReplyHa(string data,string result){
  IntPtr c;int r=app_control_create(out c);if(r!=0)return r;
  try{
   r=app_control_set_app_id(c,"com.samsung.tv.bixby-promotion");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_page","ha");
   if(r==0&&data!=null)r=app_control_add_extra_data(c,"quiet_ha_data",data);
   if(r==0&&result!=null)r=app_control_add_extra_data(c,"quiet_ha_result",result);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
   return r;
  }finally{app_control_destroy(c);}
 }
 static string FetchAlertsJson(){
  try{
   var handler=new HttpClientHandler{AutomaticDecompression=System.Net.DecompressionMethods.GZip|System.Net.DecompressionMethods.Deflate};
   using(var client=new HttpClient(handler)){
    client.Timeout=TimeSpan.FromSeconds(10);client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    return client.GetStringAsync("https://paris-mobilites.fr/api_map_alerts.php").GetAwaiter().GetResult();
   }
  }catch{return null;}
 }
 static string ParseLines(string json){
  if(json==null)return null;
  try{
   {
    var metros=new[]{"1","2","3","3bis","4","5","6","7","7bis","8","9","10","11","12","13","14"};
    var rers=new[]{"a","b","c","d","e"};
    var result=new List<object>();
    using(var doc=JsonDocument.Parse(json)){
     var alerts=doc.RootElement.GetProperty("alerts");
     foreach(var c in metros)result.Add(LineStat(alerts,"metro/"+c,"metro",c));
     foreach(var c in rers)result.Add(LineStat(alerts,"rer/"+c,"rer",c.ToUpperInvariant()));
    }
    return JsonSerializer.Serialize(result);
   }
  }catch{return null;}
 }
 static object LineStat(JsonElement alerts,string full,string kind,string label){
  int st=0;string hdr="",desc="";
  foreach(var a in alerts.EnumerateArray()){
   try{
    if(a.TryGetProperty("ended",out var e)&&e.ValueKind==JsonValueKind.True)continue;
    if(a.TryGetProperty("equipment",out var eq)&&eq.ValueKind==JsonValueKind.True)continue;
    if(!a.TryGetProperty("lines",out var ls)||ls.ValueKind!=JsonValueKind.Array)continue;
    bool match=false;foreach(var l in ls.EnumerateArray())if(l.ValueKind==JsonValueKind.String&&string.Equals(l.GetString(),full,StringComparison.OrdinalIgnoreCase)){match=true;break;}
    if(!match)continue;
    int sev=a.TryGetProperty("severity",out var sv)&&sv.ValueKind==JsonValueKind.Number?sv.GetInt32():3;
    int s=sev>=4?2:1;
    if(s>st||hdr==""){if(s>st)st=s;
     if(a.TryGetProperty("header",out var h2)&&h2.ValueKind==JsonValueKind.String)hdr=h2.GetString()??hdr;
     if(a.TryGetProperty("description",out var d2)&&d2.ValueKind==JsonValueKind.String)desc=d2.GetString()??desc;}
   }catch{}
  }
  return new{kind,label,status=st,header=hdr,desc=desc};
 }
 static WeatherSummary FetchWeather(){
  var w=FetchWeatherOpenMeteo();
  if(w!=null&&w.Days.Count>0)return w;
  var m=FetchWeatherMetNo();
  return m!=null&&m.Days.Count>0?m:w;
 }
 static WeatherSummary FetchWeatherOpenMeteo(){
  var result=new WeatherSummary();
  const string url="https://api.open-meteo.com/v1/forecast?latitude=48.8566&longitude=2.3522&current=temperature_2m,weather_code&hourly=temperature_2m,apparent_temperature,precipitation_probability,wind_speed_10m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=Europe%2FParis&forecast_days=7";
  string json=null;
  for(int attempt=0;attempt<3&&string.IsNullOrEmpty(json);attempt++){
   if(attempt>0)System.Threading.Thread.Sleep(1200);
   try{using(var client=new HttpClient()){client.Timeout=TimeSpan.FromSeconds(6);json=client.GetStringAsync(url).GetAwaiter().GetResult();}}catch{json=null;}
  }
  if(string.IsNullOrEmpty(json))return null;
  try{
    using(var data=JsonDocument.Parse(json)){
     var root=data.RootElement;var current=root.GetProperty("current");var hourly=root.GetProperty("hourly");var daily=root.GetProperty("daily");
     double temperature=current.GetProperty("temperature_2m").GetDouble();result.Code=current.GetProperty("weather_code").GetInt32();
     DateTime localNow;if(!DateTime.TryParse(current.GetProperty("time").GetString(),CultureInfo.InvariantCulture,DateTimeStyles.None,out localNow))localNow=DateTime.Now;
     var hourTimes=hourly.GetProperty("time");var hourTemps=hourly.GetProperty("temperature_2m");var hourFeels=hourly.GetProperty("apparent_temperature");var hourRain=hourly.GetProperty("precipitation_probability");var hourWind=hourly.GetProperty("wind_speed_10m");var hourCodes=hourly.GetProperty("weather_code");
     string format="0";var culture=CultureInfo.InvariantCulture;
     for(int i=0;i<hourTimes.GetArrayLength();i++){
      DateTime time;if(!DateTime.TryParse(hourTimes[i].GetString(),CultureInfo.InvariantCulture,DateTimeStyles.None,out time)||time.AddHours(1)<=localNow)continue;
      result.Hours.Add(new WeatherHour{
       Time=time.ToString("HH:mm",CultureInfo.InvariantCulture),
       Day=time.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),
       Temperature=Math.Round(hourTemps[i].GetDouble()).ToString(format,culture)+"°",
       FeelsLike=Math.Round(hourFeels[i].GetDouble()).ToString(format,culture)+"°",
       Precipitation=Math.Round(hourRain[i].GetDouble()).ToString(format,culture)+"%",
       Wind=Math.Round(hourWind[i].GetDouble()).ToString(format,culture),
       Code=hourCodes[i].GetInt32()
      });
     }
     var dates=daily.GetProperty("time");var codes=daily.GetProperty("weather_code");var highs=daily.GetProperty("temperature_2m_max");var lows=daily.GetProperty("temperature_2m_min");
     double high=highs[0].GetDouble();double low=lows[0].GetDouble();
     result.Temperature=Math.Round(temperature).ToString(format,culture)+"°";
     result.Condition=WeatherDescription(result.Code);
     result.High=Math.Round(high).ToString(format,culture)+"°";
     result.Low=Math.Round(low).ToString(format,culture)+"°";
     for(int i=0;i<Math.Min(7,dates.GetArrayLength());i++){
      int code=codes[i].GetInt32();
      result.Days.Add(new WeatherDay{
       Date=dates[i].GetString()??"",Code=code,
       High=Math.Round(highs[i].GetDouble()).ToString(format,culture)+"°",
       Low=Math.Round(lows[i].GetDouble()).ToString(format,culture)+"°",
       Condition=WeatherDescription(code)
      });
     }
    }
  }catch{return null;}
  return result;
 }
 static WeatherSummary FetchWeatherMetNo(){
  const string url="https://api.met.no/weatherapi/locationforecast/2.0/complete?lat=48.86&lon=2.35";
  string json=null;
  for(int attempt=0;attempt<2&&string.IsNullOrEmpty(json);attempt++){
   if(attempt>0)System.Threading.Thread.Sleep(1000);
   try{using(var client=new HttpClient()){client.Timeout=TimeSpan.FromSeconds(7);client.DefaultRequestHeaders.Add("User-Agent","BelAirOS-TV/1.0");json=client.GetStringAsync(url).GetAwaiter().GetResult();}}catch{json=null;}
  }
  if(string.IsNullOrEmpty(json))return null;
  try{
   var result=new WeatherSummary();var culture=CultureInfo.InvariantCulture;string fmt="0";
   using(var data=JsonDocument.Parse(json)){
    var series=data.RootElement.GetProperty("properties").GetProperty("timeseries");if(series.GetArrayLength()==0)return null;
    DateTime localNow=DateTime.Now;
    var inst0=series[0].GetProperty("data").GetProperty("instant").GetProperty("details");
    result.Code=MetNoCode(MetNoSymbol(series[0]));result.Condition=WeatherDescription(result.Code);
    result.Temperature=Math.Round(inst0.GetProperty("air_temperature").GetDouble()).ToString(fmt,culture)+"°";
    var dayMin=new Dictionary<string,double>();var dayMax=new Dictionary<string,double>();var dayCode=new Dictionary<string,int>();var dayOrder=new List<string>();
    for(int i=0;i<series.GetArrayLength();i++){
     var el=series[i];DateTime t;
     if(!DateTime.TryParse(el.GetProperty("time").GetString(),CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal,out t))continue;
     t=t.ToLocalTime();
     var det=el.GetProperty("data").GetProperty("instant").GetProperty("details");
     double temp=det.GetProperty("air_temperature").GetDouble();
     double wind=det.TryGetProperty("wind_speed",out var ws)?ws.GetDouble()*3.6:0;
     int code=MetNoCode(MetNoSymbol(el));
     if(t.AddHours(1)>localNow&&result.Hours.Count<96)result.Hours.Add(new WeatherHour{
      Time=t.ToString("HH:mm",CultureInfo.InvariantCulture),Day=t.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),
      Temperature=Math.Round(temp).ToString(fmt,culture)+"°",FeelsLike=Math.Round(temp).ToString(fmt,culture)+"°",
      Precipitation=MetNoPrecipPct(MetNoPrecip(el)),Wind=Math.Round(wind).ToString(fmt,culture),Code=code});
     string day=t.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
     if(!dayMin.ContainsKey(day)){dayMin[day]=temp;dayMax[day]=temp;dayOrder.Add(day);}else{if(temp<dayMin[day])dayMin[day]=temp;if(temp>dayMax[day])dayMax[day]=temp;}
     if(t.Hour>=12&&t.Hour<=14||!dayCode.ContainsKey(day))dayCode[day]=code;
    }
    foreach(var day in dayOrder){if(result.Days.Count>=7)break;int code=dayCode.TryGetValue(day,out var cc)?cc:result.Code;
     result.Days.Add(new WeatherDay{Date=day,Code=code,High=Math.Round(dayMax[day]).ToString(fmt,culture)+"°",Low=Math.Round(dayMin[day]).ToString(fmt,culture)+"°",Condition=WeatherDescription(code)});}
    if(result.Days.Count>0){result.High=result.Days[0].High;result.Low=result.Days[0].Low;}
   }
   return result.Days.Count>0?result:null;
  }catch{return null;}
 }
 static string MetNoSymbol(JsonElement el){
  var data=el.GetProperty("data");
  foreach(var k in new[]{"next_1_hours","next_6_hours","next_12_hours"})
   if(data.TryGetProperty(k,out var nx)&&nx.TryGetProperty("summary",out var sm)&&sm.TryGetProperty("symbol_code",out var sc))return sc.GetString()??"";
  return "";
 }
 static double MetNoPrecip(JsonElement el){
  var data=el.GetProperty("data");
  foreach(var k in new[]{"next_1_hours","next_6_hours"})
   if(data.TryGetProperty(k,out var nx)&&nx.TryGetProperty("details",out var de)&&de.TryGetProperty("precipitation_amount",out var pa))return pa.GetDouble();
  return 0;
 }
 static string MetNoPrecipPct(double mm){int p=mm<=0?0:mm<0.2?20:mm<1?50:mm<3?75:90;return p.ToString(CultureInfo.InvariantCulture)+"%";}
 static int MetNoCode(string sym){
  if(string.IsNullOrEmpty(sym))return 3;
  int u=sym.IndexOf('_');string b=u>=0?sym.Substring(0,u):sym;
  switch(b){
   case "clearsky":return 0;case "fair":return 1;case "partlycloudy":return 2;case "cloudy":return 3;case "fog":return 45;
   case "lightrainshowers":case "rainshowers":case "heavyrainshowers":return 80;
   case "lightrain":case "drizzle":return 51;case "rain":return 63;case "heavyrain":return 65;
   case "lightsleet":case "sleet":case "heavysleet":case "lightsleetshowers":case "sleetshowers":case "heavysleetshowers":return 67;
   case "lightsnow":case "snow":case "heavysnow":case "lightsnowshowers":case "snowshowers":case "heavysnowshowers":return 73;
  }
  if(b.Contains("thunder"))return 95;if(b.Contains("snow"))return 73;if(b.Contains("sleet"))return 67;if(b.Contains("rain")||b.Contains("showers"))return 63;if(b.Contains("fog"))return 45;
  return 3;
 }
 static string WeatherDescription(int code){
  if(code==0)return "Ciel dégagé";
  if(code==1)return "Peu nuageux";
  if(code==2)return "Éclaircies";
  if(code==3)return "Couvert";
  if(code==45||code==48)return "Brouillard";
  if(code>=51&&code<=57)return "Bruine";
  if(code>=61&&code<=67)return "Pluie";
  if(code>=71&&code<=77)return "Neige";
  if(code>=80&&code<=82)return "Averses";
  if(code==85||code==86)return "Averses de neige";
  if(code>=95)return "Orage";
  return "Temps variable";
 }
 static MetroSummary FetchMetro(){
  var result=new MetroSummary();
  const string url="https://paris-mobilites.fr/api_map_next.php?slug=bel-air-paris";
  try{
   using(var client=new HttpClient()){
    client.Timeout=TimeSpan.FromSeconds(7);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    string json=client.GetStringAsync(url).GetAwaiter().GetResult();
    using(var data=JsonDocument.Parse(json)){
     var root=data.RootElement;
     result.Available=root.TryGetProperty("available",out var available)&&available.GetBoolean();
     if(root.TryGetProperty("updated",out var updated))result.Updated=updated.GetString()??"";
     if(root.TryGetProperty("source",out var source))result.Source=source.GetString()??"";
     if(root.TryGetProperty("next",out var next)&&next.ValueKind==JsonValueKind.Array){
      foreach(var item in next.EnumerateArray()){
       if(!item.TryGetProperty("line",out var line)||line.GetString()!="6")continue;
       if(item.TryGetProperty("canceled",out var canceled)&&canceled.GetBoolean())continue;
       result.Next.Add(new MetroDeparture{
        Destination=item.TryGetProperty("destination",out var destination)?destination.GetString()??"":"",
        Time=item.TryGetProperty("time",out var time)?time.GetString()??"":"",
        Minutes=item.TryGetProperty("minutes",out var minutes)?Math.Max(0,minutes.GetInt32()):0,
        Realtime=item.TryGetProperty("realtime",out var realtime)&&realtime.GetBoolean(),
        AtStop=item.TryGetProperty("atStop",out var atStop)&&atStop.GetBoolean()
       });
       if(result.Next.Count>=16)break;
      }
     }
     result.Available=result.Available&&result.Next.Count>0;
    }
   }
  }catch{}
  return result;
 }
 static TrafficSummary ParseTraffic(string json){
  var result=new TrafficSummary();
  if(json==null)return result;
  try{
   {
    using(var data=JsonDocument.Parse(json)){
     var root=data.RootElement;
     result.Available=root.TryGetProperty("alerts",out var alerts)&&alerts.ValueKind==JsonValueKind.Array;
     if(root.TryGetProperty("updated",out var updated)&&updated.ValueKind==JsonValueKind.String)result.Updated=updated.GetString()??"";
     if(result.Available)foreach(var alert in alerts.EnumerateArray()){
      try{
       if(alert.TryGetProperty("ended",out var ended)&&ended.ValueKind==JsonValueKind.True)continue;
       if(alert.TryGetProperty("equipment",out var equipment)&&equipment.ValueKind==JsonValueKind.True)continue;
       var now=DateTimeOffset.Now;DateTimeOffset starts,ends;
       if(alert.TryGetProperty("start",out var start)&&start.ValueKind==JsonValueKind.String&&DateTimeOffset.TryParse(start.GetString(),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out starts)&&starts>now)continue;
       if(alert.TryGetProperty("end",out var end)&&end.ValueKind==JsonValueKind.String&&DateTimeOffset.TryParse(end.GetString(),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out ends)&&ends<now)continue;
       if(!alert.TryGetProperty("lines",out var lines)||lines.ValueKind!=JsonValueKind.Array)continue;
       bool lineSix=false;foreach(var line in lines.EnumerateArray())if(line.ValueKind==JsonValueKind.String&&string.Equals(line.GetString(),"metro/6",StringComparison.OrdinalIgnoreCase)){lineSix=true;break;}
       if(!lineSix)continue;
       result.HasAlert=true;
       if(alert.TryGetProperty("header",out var header)&&header.ValueKind==JsonValueKind.String)result.Header=header.GetString()??"Perturbation sur la ligne 6";
       if(alert.TryGetProperty("description",out var description)&&description.ValueKind==JsonValueKind.String)result.Description=description.GetString()??"";
       if(alert.TryGetProperty("effect",out var effect)&&effect.ValueKind==JsonValueKind.String)result.Effect=effect.GetString()??"Information trafic";
       break;
      }catch{continue;}
     }
    }
   }
  }catch{}
  return result;
 }
 static HttpClient WebClient(int seconds){
  var h=new HttpClientHandler{AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate};
  var c=new HttpClient(h){Timeout=TimeSpan.FromSeconds(seconds)};
  c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (SMART-TV; Linux; Tizen 9.0) AppleWebKit/537.36 (KHTML, like Gecko) Version/9.0 TV Safari/537.36");
  c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9");
  return c;
 }
 // franceinfo (public broadcaster): free full articles and a photo on every item.
 static NewsSummary FetchNews(){
  var result=new NewsSummary{Updated=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture)};
  try{
   using(var client=WebClient(9)){
    var tasks=new[]{
     FetchNewsSection(client,"Paris","France 3 Paris","https://france3-regions.francetvinfo.fr/paris-ile-de-france/rss"),
     FetchNewsSection(client,"France","franceinfo","https://www.francetvinfo.fr/france.rss"),
     FetchNewsSection(client,"Monde","franceinfo","https://www.francetvinfo.fr/monde.rss"),
     FetchNewsSection(client,"Bourse","franceinfo","https://www.francetvinfo.fr/economie/bourse.rss")
    };
    Task.WaitAll(tasks);
    result.Sections.AddRange(tasks.Select(task=>task.Result));
    // Thumbnails go to shared/trusted: same-author apps (Quiet Home) may read it, and Home has no network access.
    var all=result.Sections.SelectMany(section=>section.Items).Where(item=>item.Image.StartsWith("http")).ToList();
    Task.WaitAll(all.Select(item=>Task.Run(()=>{item.Image=CacheImage(client,item.Image,"news");})).ToArray());
    PruneDir(SharedDir("news"),new HashSet<string>(result.Sections.SelectMany(section=>section.Items).Where(item=>item.Image!="").Select(item=>System.IO.Path.GetFileName(item.Image))),0);
   }
  }catch{}
  return result;
 }
 static async Task<NewsSection> FetchNewsSection(HttpClient client,string name,string source,string url){
  var section=new NewsSection{Name=name};
  try{
   using(var response=await client.GetAsync(url)){
    response.EnsureSuccessStatusCode();
    string xml=await response.Content.ReadAsStringAsync();
    var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null};
    using(var input=new StringReader(xml))
    using(var reader=XmlReader.Create(input,settings)){
     var document=XDocument.Load(reader);
     foreach(var node in document.Descendants().Where(element=>element.Name.LocalName=="item"||element.Name.LocalName=="entry")){
      string title=CleanNewsText(ChildValue(node,"title"));
      if(title=="")continue;
      string summary=CleanNewsText(ChildValue(node,"description","summary","encoded","content"));
      string link=ChildValue(node,"link");
      if(link==""){
       var linkNode=node.Elements().FirstOrDefault(element=>element.Name.LocalName=="link");
       if(linkNode!=null)link=(string)linkNode.Attribute("href")??"";
      }
      if(link.Contains("/replay-jt/")||link.Contains("/replay-magazine/"))continue; // TV replays: video only, no text
      string image="";
      foreach(var element in node.Elements()){
       string local=element.Name.LocalName,type=(string)element.Attribute("type")??"";
       if((local=="enclosure"||local=="content"||local=="thumbnail")&&element.Attribute("url")!=null&&(type==""||type.StartsWith("image"))){image=(string)element.Attribute("url");break;}
      }
      string published=ChildValue(node,"pubDate","published","updated","date");
      DateTimeOffset parsed;
      if(DateTimeOffset.TryParse(published,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out parsed))published=parsed.ToLocalTime().ToString("dd/MM HH:mm",CultureInfo.GetCultureInfo("fr-FR"));
      if(title.Length>165)title=title.Substring(0,162)+"…";
      if(summary.Length>600)summary=summary.Substring(0,597)+"…";
      if(link.Length>500)link="";
      section.Items.Add(new NewsArticle{Title=title,Summary=summary,Url=link,Published=published,Source=source,Image=image});
      if(section.Items.Count>=5)break;
     }
    }
   }
  }catch{}
  return section;
 }
 static string sharedRoot;
 static string SharedDir(string sub){
  string root=string.IsNullOrEmpty(sharedRoot)?"/opt/usr/home/owner/apps_rw/com.samsung.tv.quicklaunch/shared/trusted/":sharedRoot;
  string dir=System.IO.Path.Combine(root,sub);Directory.CreateDirectory(dir);return dir;
 }
 static string Hash(string value){using(var sha=System.Security.Cryptography.SHA1.Create())return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))).Replace("-","").Substring(0,20).ToLowerInvariant();}
 static string CacheImage(HttpClient client,string url,string sub){
  try{
   string dir=SharedDir(sub),key=Hash(url);
   foreach(var ext in new[]{".jpg",".png",".webp"}){string existing=System.IO.Path.Combine(dir,key+ext);if(File.Exists(existing))return existing;}
   using(var response=client.GetAsync(url).GetAwaiter().GetResult()){
    string type=response.Content.Headers.ContentType?.MediaType??"";
    if(!response.IsSuccessStatusCode||!type.StartsWith("image/"))return "";
    var bytes=response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    if(bytes.Length<256||bytes.Length>4000000)return "";
    string path=System.IO.Path.Combine(dir,key+(type.Contains("png")?".png":type.Contains("webp")?".webp":".jpg"));
    File.WriteAllBytes(path+".part",bytes);File.Move(path+".part",path,true);
    return path;
   }
  }catch{return "";}
 }
 static void PruneDir(string dir,HashSet<string> keep,int keepNewest){
  try{var files=new System.IO.DirectoryInfo(dir).GetFiles().OrderByDescending(file=>file.LastWriteTimeUtc).ToList();
   for(int i=keepNewest;i<files.Count;i++)if(!keep.Contains(files[i].Name))files[i].Delete();}catch{}
 }
 // ---- full article on demand: body paragraphs and subheadings, plus the large photo ----
 static string FetchArticleJson(string url){
  var blocks=new List<string>();string image="";Uri uri;
  if(Uri.TryCreate(url,UriKind.Absolute,out uri)&&(uri.Scheme=="https"||uri.Scheme=="http")&&(uri.Host.EndsWith("franceinfo.fr")||uri.Host.EndsWith("francetvinfo.fr"))){
   try{using(var client=WebClient(10)){
    string page=client.GetStringAsync(uri).GetAwaiter().GetResult();
    blocks=ExtractArticle(page);
    string og=WebUtility.HtmlDecode(MetaContent(page,"og:image"));
    if(og.StartsWith("http"))image=CacheImage(client,og,"article");
    PruneDir(SharedDir("article"),new HashSet<string>(),12);
   }}catch{}
  }
  return JsonSerializer.Serialize(new{Url=url,Blocks=blocks,Image=image});
 }
 static string MetaContent(string page,string property){
  var match=Regex.Match(page,"<meta[^>]+property=\""+Regex.Escape(property)+"\"[^>]+content=\"([^\"]+)\"");
  if(!match.Success)match=Regex.Match(page,"<meta[^>]+content=\"([^\"]+)\"[^>]+property=\""+Regex.Escape(property)+"\"");
  return match.Success?match.Groups[1].Value:"";
 }
 static string FindBlock(string page,string marker){
  int at=page.IndexOf(marker,StringComparison.Ordinal);if(at<0)return null;
  int start=page.LastIndexOf('<',at);if(start<0)return null;
  var tag=Regex.Match(page.Substring(start,Math.Min(40,page.Length-start)),@"^<(\w+)");if(!tag.Success)return null;
  int depth=0;
  foreach(Match match in new Regex("<(/?)"+tag.Groups[1].Value+@"\b[^>]*?(/?)>").Matches(page,start)){
   if(match.Groups[2].Value=="/")continue;
   depth+=match.Groups[1].Value=="/"?-1:1;
   if(depth==0)return page.Substring(start,match.Index+match.Length-start);
  }
  return page.Substring(start);
 }
 static List<string> ExtractArticle(string page){
  var blocks=new List<string>();string body=null;
  foreach(var marker in new[]{"class=\"c-body","itemprop=\"articleBody\"","class=\"article__body"}){body=FindBlock(page,marker);if(body!=null)break;}
  if(body==null)return blocks;
  body=Regex.Replace(body,@"<(figure|aside|script|style|button|iframe|noscript)\b.*?</\1>","",RegexOptions.Singleline);
  int total=0;
  foreach(Match match in Regex.Matches(body,@"<(p|h2|h3|li)\b[^>]*>(.*?)</\1>",RegexOptions.Singleline)){
   string kind=match.Groups[1].Value,inner=match.Groups[2].Value.Trim();
   if(kind=="p"&&Regex.IsMatch(inner,@"^<a\b[^>]*>.*</a>$",RegexOptions.Singleline))continue; // paragraph that is only a link to another article
   string text=CleanNewsText(inner);
   if(text.Length<2||Regex.IsMatch(text,"^(Lire aussi|À lire aussi|Article rédigé par|Publié le|Partager)",RegexOptions.IgnoreCase))continue;
   blocks.Add(kind.StartsWith("h")?"#"+text:kind=="li"?"• "+text:text);
   total+=text.Length;if(total>16000)break;
  }
  return blocks;
 }
 static int ReplyArticle(string json){
  IntPtr c;int r=app_control_create(out c);if(r!=0)return r;
  try{
   r=app_control_set_app_id(c,"com.samsung.tv.bixby-promotion");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_page","article");
   if(r==0)r=app_control_add_extra_data(c,"quiet_article",json);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
   return r;
  }finally{app_control_destroy(c);}
 }
 // ---- electricity: daily kWh and cost for the last 30 days from Home Assistant long-term statistics (WebSocket only) ----
 static bool IsPrivateHost(string host){
  IPAddress ip;if(!IPAddress.TryParse(host,out ip))return host.EndsWith(".local");
  var b=ip.GetAddressBytes();return b.Length==4&&(b[0]==10||b[0]==192&&b[1]==168||b[0]==172&&b[1]>=16&&b[1]<=31);
 }
 static void WsSend(System.Net.WebSockets.ClientWebSocket ws,string text,System.Threading.CancellationToken ct){
  ws.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(text)),System.Net.WebSockets.WebSocketMessageType.Text,true,ct).GetAwaiter().GetResult();
 }
 static string WsRecv(System.Net.WebSockets.ClientWebSocket ws,System.Threading.CancellationToken ct){
  var buffer=new byte[65536];using(var ms=new MemoryStream()){
   while(true){var r=ws.ReceiveAsync(new ArraySegment<byte>(buffer),ct).GetAwaiter().GetResult();ms.Write(buffer,0,r.Count);if(r.EndOfMessage||r.MessageType==System.Net.WebSockets.WebSocketMessageType.Close)break;}
   return System.Text.Encoding.UTF8.GetString(ms.ToArray());
  }
 }
 // Runs one authenticated WebSocket session against the first Home Assistant URL that answers.
 static T WithHaSocket<T>(Func<System.Net.WebSockets.ClientWebSocket,System.Threading.CancellationToken,T> work,int seconds) where T:class{
  LoadHaConfig();
  foreach(var b in haUrls){
   try{
    bool local=IsPrivateHost(new Uri(b).Host);
    using(var cts=new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(seconds)))
    using(var ws=new System.Net.WebSockets.ClientWebSocket()){
     ws.Options.RemoteCertificateValidationCallback=(sender,cert,chain,errors)=>local||errors==System.Net.Security.SslPolicyErrors.None; // owner-authorised: local HA uses a self-signed certificate
     ws.ConnectAsync(new Uri((b.StartsWith("https")?"wss":"ws")+b.Substring(b.IndexOf("://",StringComparison.Ordinal))+"/api/websocket"),cts.Token).GetAwaiter().GetResult();
     WsRecv(ws,cts.Token);
     WsSend(ws,JsonSerializer.Serialize(new{type="auth",access_token=haToken}),cts.Token);
     if(!WsRecv(ws,cts.Token).Contains("auth_ok"))continue;
     var result=work(ws,cts.Token);
     if(result!=null)return result;
    }
   }catch{}
  }
  return null;
 }
 static string FetchEnergyJson(){
  return WithHaSocket((ws,ct)=>{
     WsSend(ws,"{\"id\":1,\"type\":\"recorder/list_statistic_ids\",\"statistic_type\":\"sum\"}",ct);
     var energyIds=new List<string>();var costIds=new List<string>();var fallbackEnergy=new List<string>();var fallbackCost=new List<string>();
     using(var doc=JsonDocument.Parse(WsRecv(ws,ct)))foreach(var st in doc.RootElement.GetProperty("result").EnumerateArray()){
      string id=st.GetProperty("statistic_id").GetString()??"";
      if(id.StartsWith("octopus_french:")&&id.Contains("_energy"))energyIds.Add(id);
      else if(id.StartsWith("octopus_french:")&&id.Contains("_cost"))costIds.Add(id);
      else if(id.StartsWith("sensor.linky_")&&id.EndsWith("_consommation"))fallbackEnergy.Add(id);
      else if(id.StartsWith("sensor.linky_")&&id.EndsWith("_cout"))fallbackCost.Add(id);
     }
     if(energyIds.Count==0){energyIds=fallbackEnergy;costIds=fallbackCost;}
     if(energyIds.Count==0)return null;
     string start=DateTime.Today.AddDays(-31).ToUniversalTime().ToString("o",CultureInfo.InvariantCulture);
     WsSend(ws,JsonSerializer.Serialize(new{id=2,type="recorder/statistics_during_period",start_time=start,statistic_ids=energyIds.Concat(costIds).ToArray(),period="day",types=new[]{"change"}}),ct);
     var kwh=new SortedDictionary<string,double>();var eur=new Dictionary<string,double>();
     using(var doc=JsonDocument.Parse(WsRecv(ws,ct)))foreach(var series in doc.RootElement.GetProperty("result").EnumerateObject()){
      bool isCost=costIds.Contains(series.Name);
      foreach(var row in series.Value.EnumerateArray()){
       if(!row.TryGetProperty("change",out var ch)||ch.ValueKind!=JsonValueKind.Number)continue;
       string day=DateTimeOffset.FromUnixTimeMilliseconds((long)row.GetProperty("start").GetDouble()).ToLocalTime().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
       var target=isCost?eur:(IDictionary<string,double>)kwh;target[day]=(target.TryGetValue(day,out var prev)?prev:0)+ch.GetDouble();
      }
     }
     var days=kwh.Where(kv=>string.CompareOrdinal(kv.Key,DateTime.Today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture))<0).Reverse().Take(30).Reverse()
      .Select(kv=>new{Date=kv.Key,Kwh=Math.Round(kv.Value,3),Eur=Math.Round(eur.TryGetValue(kv.Key,out var c)?c:0,3)}).ToList();
     return JsonSerializer.Serialize(days);
  },15);
 }
 // ---- media: what plays on the Apple TV, plus Plex "continue watching" and "on deck" read through Home Assistant ----
 const string plexClient="media_player.plex_plex_for_samsung_tv_2025";
 // Home Assistant can only read the Plex library through a Plex player it currently sees: the Apple TV's, else the TV's.
 static readonly string[] plexBrowsers={"media_player.plex_plex_for_apple_tv_apple_tv",plexClient};static string browseClient=plexClient;
 static bool PickPlexBrowser(){foreach(var e in plexBrowsers){string js=HaGet("/api/states/"+e);if(js==null)continue;try{using(var d=JsonDocument.Parse(js)){string st=Str(d.RootElement,"state");if(st!=""&&st!="unavailable"){browseClient=e;return true;}}}catch{}}return false;}
 static string Str(JsonElement el,string name){return el.ValueKind==JsonValueKind.Object&&el.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";}
 static double Num(JsonElement el,string name){return el.ValueKind==JsonValueKind.Object&&el.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.Number?v.GetDouble():0;}
 static byte[] HaGetBytes(string path){
  LoadHaConfig();
  foreach(var b in haUrls){try{using(var resp=HaClient().GetAsync(b+path).GetAwaiter().GetResult()){if(resp.IsSuccessStatusCode)return resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();}}catch{}}
  return null;
 }
 // Home Assistant image (Plex artwork, player artwork, robot map) cached once under a stable key in the shared folder Home reads.
 static string HaImage(string path,string sub,string key){
  try{
   string dir=SharedDir(sub);
   foreach(var ext in new[]{".jpg",".png"}){string existing=System.IO.Path.Combine(dir,key+ext);if(File.Exists(existing))return existing;}
   var bytes=HaGetBytes(path);
   if(bytes==null||bytes.Length<256||bytes.Length>8000000)return "";
   bool png=bytes[0]==0x89&&bytes[1]==0x50;
   string file=System.IO.Path.Combine(dir,key+(png?".png":".jpg"));
   File.WriteAllBytes(file+".part",bytes);File.Move(file+".part",file,true);
   return file;
  }catch{return "";}
 }
 static string BrowseMedia(System.Net.WebSockets.ClientWebSocket ws,System.Threading.CancellationToken ct,int id,string type,string contentId){
  string request=type==null?JsonSerializer.Serialize(new{id,type="media_player/browse_media",entity_id=browseClient})
   :JsonSerializer.Serialize(new{id,type="media_player/browse_media",entity_id=browseClient,media_content_type=type,media_content_id=contentId});
  WsSend(ws,request,ct);
  string reply=WsRecv(ws,ct);
  return reply.Contains("\"success\":true")?reply:null;
 }
 static object NowPlaying(){
  foreach(var entity in new[]{"media_player.appletv_appletv",plexClient}){
   string json=HaGet("/api/states/"+entity);if(json==null)continue;
   try{using(var doc=JsonDocument.Parse(json)){
    var root=doc.RootElement;string state=Str(root,"state");
    if(state!="playing"&&state!="paused")continue;
    var a=root.GetProperty("attributes");string title=Str(a,"media_title");if(title=="")continue;
    string sub=Str(a,"media_series_title");
    if(sub!=""){int season=(int)Num(a,"media_season"),episode=(int)Num(a,"media_episode");string ep=season>0&&episode>0?"S"+season.ToString("00")+"E"+episode.ToString("00"):"";title=sub;sub=ep!=""?ep+" · "+Str(a,"media_title"):Str(a,"media_title");}
    else sub=Str(a,"media_artist");
    string picture=Str(a,"entity_picture"),image="";
    if(picture.StartsWith("/api/")){var cache=Regex.Match(picture,"[?&]cache=([^&]+)");image=HaImage(picture,"now",Hash(entity+(cache.Success?cache.Groups[1].Value:picture)));PruneDir(SharedDir("now"),new HashSet<string>{System.IO.Path.GetFileName(image)},6);}
    return new{Source=entity.Contains("appletv")?"Apple TV":"Plex",App=Str(a,"app_name"),Title=title,Sub=sub,State=state,Pos=Num(a,"media_position"),Dur=Num(a,"media_duration"),Updated=Str(a,"media_position_updated_at"),Image=image};
   }}catch{}
  }
  return null;
 }
 static string FetchMediaJson(bool hubs){
  var now=NowPlaying();
  List<Dictionary<string,string>> items=null;
  if(hubs){
   var raw=!PickPlexBrowser()?null:WithHaSocket((ws,ct)=>{
    string rootReply=BrowseMedia(ws,ct,1,null,null);if(rootReply==null)return null;
    string root;using(var doc=JsonDocument.Parse(rootReply))root=Str(doc.RootElement.GetProperty("result"),"media_content_id");
    if(root=="")return null;
    var list=new List<Dictionary<string,string>>();var seen=new HashSet<string>();int id=2;
    var hubList=new List<string>{"home.continue","home.ondeck","home.movies.recent","home.television.recent"};
    int recent=0;
    foreach(var hub in hubList){
     bool isRecent=hub.EndsWith(".recent");if(!isRecent&&list.Count>=8)continue;if(isRecent&&recent>=10)continue;
     string reply=BrowseMedia(ws,ct,id++,"hub",root+"/"+hub);if(reply==null)continue;
     using(var doc=JsonDocument.Parse(reply)){
      if(!doc.RootElement.GetProperty("result").TryGetProperty("children",out var children)||children.ValueKind!=JsonValueKind.Array)continue;
      foreach(var c in children.EnumerateArray()){
       string cid=Str(c,"media_content_id");if(cid==""||!seen.Add(cid.Split('?')[0]))continue;
       string title=Str(c,"title"),type=Str(c,"media_content_type"),sub="";
       var ep=Regex.Match(title,@"^(.*?) - (S\d+E\d+) - (.*)$");var movie=Regex.Match(title,@"^(.*) \((\d{4})\)$");
       if(ep.Success){title=ep.Groups[1].Value;sub=ep.Groups[2].Value+" · "+ep.Groups[3].Value;}
       else if(movie.Success){title=movie.Groups[1].Value;sub="Film · "+movie.Groups[2].Value;}
       else sub=type=="movie"?"Film":type=="episode"?"Épisode":"";
       list.Add(new Dictionary<string,string>{{"Id",cid},{"Type",type},{"Title",title},{"Sub",sub},{"Thumb",Str(c,"thumbnail")},{"Hub",isRecent?"recent":"continue"}});
       if(isRecent){if(++recent>=(hub.Contains("movies")?5:10))break;}else if(list.Count(x=>x["Hub"]=="continue")>=8)break;
      }
     }
    }
    return list;
   },20);
   if(raw!=null){
    Task.WaitAll(raw.Select(item=>Task.Run(()=>{string t=item["Thumb"];item["Image"]=t.StartsWith("/api/")?HaImage(t,"plex",Hash(t)):"";item.Remove("Thumb");})).ToArray());
    PruneDir(SharedDir("plex"),new HashSet<string>(raw.Select(item=>System.IO.Path.GetFileName(item["Image"]))),40);
    items=raw;
    try{File.WriteAllText(System.IO.Path.Combine(SharedDir("plex"),"hubs.json"),JsonSerializer.Serialize(raw));}catch{}
   }
   else{
    // No Plex player is reachable (Plex closed everywhere): keep showing the last lists.
    try{string cached=System.IO.Path.Combine(SharedDir("plex"),"hubs.json");if(File.Exists(cached))items=JsonSerializer.Deserialize<List<Dictionary<string,string>>>(File.ReadAllText(cached));}catch{}
   }
  }
  return JsonSerializer.Serialize(new{Now=now,Items=items});
 }
 // Home opens the Plex app first; the client needs a few seconds before it accepts a play command.
 static void PlayOnPlex(string request){
  var parts=request.Split('\t');if(parts.Length<2||!parts[0].StartsWith("plex://"))return;
  string body=JsonSerializer.Serialize(new{entity_id=plexClient,media_content_id=parts[0],media_content_type=parts[1]});
  System.Threading.Thread.Sleep(5000);
  for(int attempt=0;attempt<8;attempt++){
   if(HaPost("/api/services/media_player/play_media",body))break;
   System.Threading.Thread.Sleep(3000);
  }
 }
 // ---- Félix: live map from the Roborock image entity, plus every Félix state and routine button ----
 static List<string[]> felixRooms;
 static List<string[]> FelixRooms(){
  if(felixRooms!=null&&felixRooms.Count>0)return felixRooms;
  var list=new List<string[]>();
  try{string reply=HaPostJson("/api/services/roborock/get_maps?return_response","{\"entity_id\":\"vacuum.felix\"}");
   if(reply!=null)using(var doc=JsonDocument.Parse(reply)){
    var maps=doc.RootElement.GetProperty("service_response").GetProperty("vacuum.felix").GetProperty("maps");
    if(maps.GetArrayLength()>0&&maps[0].TryGetProperty("rooms",out var rooms))foreach(var r in rooms.EnumerateObject())list.Add(new[]{r.Name,r.Value.GetString()??r.Name});
   }}catch{}
  felixRooms=list;return list;
 }
 static DateTime felixForced=DateTime.MinValue;
 static string FetchFelixJson(bool live){
  // While Félix works, ask Home Assistant to re-read the robot (at most every 16 s) so the map follows him closely.
  if(live&&(DateTime.UtcNow-felixForced).TotalSeconds>=16){felixForced=DateTime.UtcNow;HaPost("/api/services/homeassistant/update_entity","{\"entity_id\":[\"vacuum.felix\",\"image.felix_map_0\"]}");System.Threading.Thread.Sleep(1500);}
  string json=HaGet("/api/states");if(json==null)return null;
  var states=new Dictionary<string,string>();var buttons=new List<string[]>();string stamp="";
  try{using(var doc=JsonDocument.Parse(json))foreach(var s in doc.RootElement.EnumerateArray()){
   string id=Str(s,"entity_id");
   if(id=="image.felix_map_0")stamp=Str(s,"state");
   else if(id.StartsWith("button.felix_")){string name=Str(s.GetProperty("attributes"),"friendly_name");if(name.StartsWith("Félix "))name=name.Substring(6);buttons.Add(new[]{id,name});}
   else if(id=="vacuum.felix"||id.StartsWith("sensor.felix_")||id.StartsWith("binary_sensor.felix_"))states[id]=Str(s,"state");
  }}catch{return null;}
  string map=stamp==""?"":HaImage("/api/image_proxy/image.felix_map_0","felix","map-"+Hash(stamp));
  PruneDir(SharedDir("felix"),new HashSet<string>{System.IO.Path.GetFileName(map)},2);
  return JsonSerializer.Serialize(new{Map=map,Stamp=stamp,States=states,Buttons=buttons,Rooms=FelixRooms()});
 }
 static int ReplyExtra(string media,string felix,string series=null,string facts=null,string mobility=null,string rain=null,string basemap=null,string find=null){
  if(media==null&&felix==null&&series==null&&facts==null&&mobility==null&&rain==null&&basemap==null&&find==null)return 0;
  IntPtr c;int r=app_control_create(out c);if(r!=0)return r;
  try{
   r=app_control_set_app_id(c,"com.samsung.tv.bixby-promotion");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_page","extra");
   if(r==0&&media!=null)r=app_control_add_extra_data(c,"quiet_media",media);
   if(r==0&&felix!=null)r=app_control_add_extra_data(c,"quiet_felix",felix);
   if(r==0&&series!=null)r=app_control_add_extra_data(c,"quiet_series",series);
   if(r==0&&facts!=null)r=app_control_add_extra_data(c,"quiet_facts",facts);
   if(r==0&&mobility!=null)r=app_control_add_extra_data(c,"quiet_mobility",mobility);
   if(r==0&&rain!=null)r=app_control_add_extra_data(c,"quiet_rain",rain);
   if(r==0&&basemap!=null)r=app_control_add_extra_data(c,"quiet_basemap",basemap);
   if(r==0&&find!=null)r=app_control_add_extra_data(c,"quiet_find",find);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
   return r;
  }finally{app_control_destroy(c);}
 }
 // ---- live market quotes (Yahoo Finance chart endpoint, no key): last price, previous close, intraday points ----
 static List<MarketQuote> FetchMarkets(){
  var list=new List<MarketQuote>();
  var symbols=new[]{new[]{"^FCHI","CAC 40"},new[]{"^STOXX50E","Euro Stoxx 50"},new[]{"^GSPC","S&P 500"},new[]{"QQQ","QQQ · Nasdaq 100"},
   new[]{"NVDA","NVIDIA"},new[]{"AMD","AMD"},new[]{"MU","Micron"},new[]{"SNDK","SanDisk"},new[]{"GOOGL","Alphabet (Google)"},new[]{"AAPL","Apple"},
   new[]{"GC=F","Or · once"},new[]{"HG=F","Cuivre · livre"},new[]{"EURUSD=X","EUR/USD"}};
  try{using(var client=WebClient(8)){
   var tasks=symbols.Select(symbol=>Task.Run(()=>{
    try{
     string json=client.GetStringAsync("https://query1.finance.yahoo.com/v8/finance/chart/"+Uri.EscapeDataString(symbol[0])+"?range=1d&interval=5m").GetAwaiter().GetResult();
     using(var doc=JsonDocument.Parse(json)){
      var result=doc.RootElement.GetProperty("chart").GetProperty("result")[0];var meta=result.GetProperty("meta");
      var quote=new MarketQuote{Symbol=symbol[0],Name=symbol[1],Price=meta.GetProperty("regularMarketPrice").GetDouble()};
      if(meta.TryGetProperty("chartPreviousClose",out var previous)&&previous.ValueKind==JsonValueKind.Number)quote.Previous=previous.GetDouble();
      if(meta.TryGetProperty("regularMarketTime",out var time)&&time.ValueKind==JsonValueKind.Number)quote.Time=time.GetInt64();
      if(result.TryGetProperty("indicators",out var indicators)){
       var closes=indicators.GetProperty("quote")[0].GetProperty("close");bool hasTimes=result.TryGetProperty("timestamp",out var stamps)&&stamps.GetArrayLength()==closes.GetArrayLength();
       for(int i=0;i<closes.GetArrayLength();i++)if(closes[i].ValueKind==JsonValueKind.Number){quote.Points.Add(Math.Round(closes[i].GetDouble(),5));if(hasTimes)quote.Times.Add(stamps[i].GetInt64());}
      }
      return quote;
     }
    }catch{return null;}
   })).ToArray();
   Task.WaitAll(tasks);
   foreach(var task in tasks)if(task.Result!=null)list.Add(task.Result); // keeps the order above
  }}catch{}
  return list;
 }
 // ---- price history for the chart scales: "SYMBOL|range" with range 5d, 1mo, 6mo, 1y or max ----
 static readonly Dictionary<string,string> seriesInterval=new Dictionary<string,string>{{"1d","5m"},{"5d","30m"},{"1mo","60m"},{"6mo","1d"},{"1y","1d"},{"max","1mo"}};
 static readonly HashSet<string> seriesSymbols=new HashSet<string>{"^FCHI","^STOXX50E","^GSPC","QQQ","NVDA","AMD","MU","SNDK","GOOGL","AAPL","GC=F","HG=F","EURUSD=X"};
 static string FetchSeriesJson(string request){
  var parts=request.Split('|');if(parts.Length!=2||!seriesSymbols.Contains(parts[0])||!seriesInterval.ContainsKey(parts[1]))return null;
  try{using(var client=WebClient(12)){
   string json=client.GetStringAsync("https://query1.finance.yahoo.com/v8/finance/chart/"+Uri.EscapeDataString(parts[0])+"?range="+parts[1]+"&interval="+seriesInterval[parts[1]]).GetAwaiter().GetResult();
   using(var doc=JsonDocument.Parse(json)){
    var result=doc.RootElement.GetProperty("chart").GetProperty("result")[0];var meta=result.GetProperty("meta");
    var points=new List<double>();var times=new List<long>();
    var closes=result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");bool hasTimes=result.TryGetProperty("timestamp",out var stamps)&&stamps.GetArrayLength()==closes.GetArrayLength();
    for(int i=0;i<closes.GetArrayLength();i++)if(closes[i].ValueKind==JsonValueKind.Number&&hasTimes){points.Add(Math.Round(closes[i].GetDouble(),5));times.Add(stamps[i].GetInt64());}
    double previous=meta.TryGetProperty("chartPreviousClose",out var pc)&&pc.ValueKind==JsonValueKind.Number?pc.GetDouble():points.Count>0?points[0]:0;
    if(parts[1]=="max"&&points.Count>0)previous=points[0];
    return JsonSerializer.Serialize(new{Symbol=parts[0],Range=parts[1],Price=meta.GetProperty("regularMarketPrice").GetDouble(),Previous=previous,Points=points,Times=times});
   }
  }}catch{return JsonSerializer.Serialize(new{Symbol=parts[0],Range=parts[1],Price=0.0,Previous=0.0,Points=new double[0],Times=new long[0]});}
 }
 // ---- "Le saviez-vous ?" anecdotes from French Wikipedia: today's plus last year's archive, cached a day in the shared folder ----
 static List<string> WikiAnecdotes(HttpClient client,string page){
  var list=new List<string>();
  try{
   string json=client.GetStringAsync("https://fr.wikipedia.org/w/api.php?action=parse&format=json&prop=text&formatversion=2&page="+Uri.EscapeDataString(page)).GetAwaiter().GetResult();
   using(var doc=JsonDocument.Parse(json)){
    if(!doc.RootElement.TryGetProperty("parse",out var parse))return list;
    string text=Regex.Replace(parse.GetProperty("text").GetString()??"","<(figure|figcaption)\\b.*?</\\1>|<div class=\"thumb.*?</div>\\s*</div>","",RegexOptions.Singleline);
    foreach(Match m in Regex.Matches(text,"<li>(.*?)</li>",RegexOptions.Singleline)){
     string t=CleanNewsText(m.Groups[1].Value);
     int cut=t.IndexOf(" présente ",StringComparison.Ordinal);if(cut<0)cut=t.IndexOf("présente ",StringComparison.Ordinal);if(cut>0)t=t.Substring(0,cut).Trim();
     t=Regex.Replace(t,"\\s*\\((photo|illustration|dessin|image|carte|portrait|gravure|tableau|statue|affiche|peinture|vidéo|schéma|photographie|sculpture|logo|plan|croquis|timbre|caricature)[^)]{0,20}\\)","");
     t=Regex.Replace(t,"\\s+"," ").Trim();
     if(t.Length>=40&&t.Length<=260&&(t.EndsWith(".")||t.EndsWith("?")||t.EndsWith("!")||t.EndsWith("»")))list.Add(t);
    }
   }
  }catch{}
  return list;
 }
 static string FetchFactsJson(){
  string cache=System.IO.Path.Combine(SharedDir("data"),"anecdotes.json");
  try{if(File.Exists(cache)&&(DateTime.UtcNow-File.GetLastWriteTimeUtc(cache)).TotalHours<20)return Pick(JsonSerializer.Deserialize<List<string>>(File.ReadAllText(cache)));}catch{}
  var all=new List<string>();
  try{using(var client=WebClient(20)){
   all.AddRange(WikiAnecdotes(client,"Wikipédia:Le saviez-vous ?/Anecdotes sur l'accueil"));
   int year=DateTime.Now.Year;
   foreach(int y in new[]{year,year-1}){var a=WikiAnecdotes(client,"Wikipédia:Le saviez-vous ?/Archives/"+y);all.AddRange(a);if(all.Count>300)break;}
  }}catch{}
  all=all.Distinct().ToList();
  if(all.Count<10)return null;
  try{File.WriteAllText(cache,JsonSerializer.Serialize(all));}catch{}
  return Pick(all);
 }
 // The most recent anecdotes first, then a random sample of the archive.
 static string Pick(List<string> all){
  if(all==null||all.Count==0)return null;
  var rnd=new Random();var head=all.Take(6).ToList();var rest=all.Skip(6).OrderBy(_=>rnd.Next()).Take(54);
  return JsonSerializer.Serialize(head.Concat(rest).ToList());
 }
 static string ChildValue(XElement parent,params string[] names){
  foreach(string name in names){
   var child=parent.Elements().FirstOrDefault(element=>element.Name.LocalName==name);
   if(child!=null){string value=(string)child.Attribute("href")??child.Value;if(value!="")return value.Trim();}
  }
  return "";
 }
 static string CleanNewsText(string value){
  if(string.IsNullOrWhiteSpace(value))return "";
  string text=WebUtility.HtmlDecode(value);
  text=Regex.Replace(text,"<[^>]*>"," ");
  return Regex.Replace(text,"\\s+"," ").Trim();
 }
 static int LaunchHome(WeatherSummary weather,MetroSummary metro,TrafficSummary traffic,NewsSummary news,string linesJson,string marketsJson,string energyJson){
  IntPtr c;int result=app_control_create(out c);if(result!=0)return result;
  try{
   result=app_control_set_app_id(c,"com.samsung.tv.bixby-promotion");
   if(result==0)result=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(result==0&&(weather!=null||metro!=null||traffic!=null||news!=null||linesJson!=null||marketsJson!=null||energyJson!=null))result=app_control_add_extra_data(c,"quiet_page","data");
   if(result==0&&weather!=null)result=app_control_add_extra_data(c,"quiet_temperature",weather.Temperature);
   if(result==0&&weather!=null)result=app_control_add_extra_data(c,"quiet_condition",weather.Condition);
   if(result==0&&weather!=null)result=app_control_add_extra_data(c,"quiet_high",weather.High);
   if(result==0&&weather!=null)result=app_control_add_extra_data(c,"quiet_low",weather.Low);
   if(result==0&&weather!=null)result=app_control_add_extra_data(c,"quiet_weather_code",weather.Code.ToString(CultureInfo.InvariantCulture));
   if(result==0&&weather!=null)result=app_control_add_extra_data(c,"quiet_forecast",JsonSerializer.Serialize(weather.Days,new JsonSerializerOptions{IncludeFields=true}));
   if(result==0&&weather!=null)result=app_control_add_extra_data(c,"quiet_hours",JsonSerializer.Serialize(weather.Hours,new JsonSerializerOptions{IncludeFields=true}));
   if(result==0&&metro!=null)result=app_control_add_extra_data(c,"quiet_metro",JsonSerializer.Serialize(metro,new JsonSerializerOptions{IncludeFields=true}));
   if(result==0&&traffic!=null)result=app_control_add_extra_data(c,"quiet_traffic",JsonSerializer.Serialize(traffic,new JsonSerializerOptions{IncludeFields=true}));
   if(result==0&&news!=null)result=app_control_add_extra_data(c,"quiet_news",JsonSerializer.Serialize(news,new JsonSerializerOptions{IncludeFields=true}));
   if(result==0&&linesJson!=null)result=app_control_add_extra_data(c,"quiet_lines",linesJson);
   if(result==0&&marketsJson!=null)result=app_control_add_extra_data(c,"quiet_markets",marketsJson);
   if(result==0&&energyJson!=null)result=app_control_add_extra_data(c,"quiet_energy",energyJson);
   if(result==0)result=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
   return result;
  }finally{app_control_destroy(c);}
 }
 public static void Main(string[] args){using(var app=new QuickLaunch())app.Run(args);}
}
