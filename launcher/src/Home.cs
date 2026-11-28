using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.Applications;

public sealed class QuietHome : NUIApplication
{
 const string Root="/opt/usr/apps/com.samsung.tv.bixby-promotion/res/quiet-home/";
 const string StateFile="/tmp/quiet-home-state.txt";
 static readonly string BuildTag=ComputeBuildTag();
 static string ComputeBuildTag(){try{var loc=System.Reflection.Assembly.GetExecutingAssembly().Location;if(!string.IsNullOrEmpty(loc)&&File.Exists(loc))return File.GetLastWriteTime(loc).ToString("yyyy.M.d",CultureInfo.InvariantCulture);}catch{}return DateTime.Now.ToString("yyyy.M.d",CultureInfo.InvariantCulture);}
 sealed class Slot { public View Card,Shadow,FocusRing; public TextLabel Label; public string Name,Action,Description; public int Row,X,Y,W,H; }
 sealed class CachedArtwork { public ImageView Image; public View Parent; }
 sealed class ForecastDay { public string Date,High,Low,Condition; public int Code; }
 sealed class ForecastHour { public string Time,Temperature,FeelsLike,Precipitation,Wind,Day; public int Code; }
 sealed class MetroDeparture { public string Destination,Time; public int Minutes; public bool Realtime,AtStop; }
 sealed class NewsArticle { public string Title="",Summary="",Url="",Published="",Source="",Image=""; }
 sealed class MarketQuote { public string Symbol="",Name=""; public double Price,Previous; public long Time; public List<double> Points=new List<double>(); public List<long> Times=new List<long>(); }
 sealed class EnergyDay { public string Date=""; public double Kwh,Eur; }
 sealed class ArticleFull { public List<string> Blocks=new List<string>(); public string Image=""; }
 sealed class NewsSection { public string Name=""; public List<NewsArticle> Items=new List<NewsArticle>(); }
 sealed class NewsSummary { public string Updated=""; public List<NewsSection> Sections=new List<NewsSection>(); }
 sealed class PlexItem { public string Id="",Type="",Title="",Sub="",Image="",Hub=""; }
 sealed class BikeStation { public string Name=""; public int Dist,Mech,Ebike,Docks,Dx,Dy; public bool Open=true; }
 sealed class NowItem { public string Source="",App="",Title="",Sub="",State="",Image="",Updated=""; public double Pos,Dur; }
 readonly List<PlexItem> plexItems=new List<PlexItem>();bool plexOffline;NowItem nowPlaying;bool mediaLoaded;
 readonly List<BikeStation> velib=new List<BikeStation>();readonly List<int[]> limePts=new List<int[]>();int limeCount=-1,limeNear=-1,metroDx,metroDy;string mobAt="";bool mobLoaded;DateTime lastMobilityRequest=DateTime.MinValue;
 readonly List<KeyValuePair<string,double>> rainSlots=new List<KeyValuePair<string,double>>();TextLabel weatherRain,metroFoot;
 DateTime lastMediaRequest=DateTime.MinValue,lastHubRequest=DateTime.MinValue,lastFelixRequest=DateTime.MinValue;
 TextLabel heroTop,heroTitle,heroSub,heroTime;View heroArt,heroBar,heroFill;string heroArtPath="";
 readonly Dictionary<string,string> felix=new Dictionary<string,string>();readonly List<string[]> felixButtons=new List<string[]>();string felixMap="",felixStamp="";bool felixLoaded;
 string prefTheme="auto",briefDate="";int prefAccent,prefWall;bool prefBrief=true;string wallKind="";
 View briefRoot;Tizen.NUI.Timer briefTimer;readonly Dictionary<string,TextLabel[]> briefCards=new Dictionary<string,TextLabel[]>();TextLabel briefClock;
 static readonly string[] accentNames={"Indigo","Bleu","Vert","Ambre","Rose","Graphite","Rouille"};
 static readonly float[][] accentLight={new[]{.24f,.29f,.72f},new[]{.04f,.42f,.84f},new[]{.09f,.50f,.33f},new[]{.76f,.43f,.04f},new[]{.80f,.20f,.45f},new[]{.25f,.27f,.31f},new[]{.76f,.31f,.15f}};
 static readonly float[][] accentDark={new[]{.45f,.55f,1f},new[]{.36f,.66f,1f},new[]{.35f,.82f,.58f},new[]{1f,.70f,.30f},new[]{1f,.50f,.68f},new[]{.62f,.66f,.74f},new[]{.98f,.56f,.29f}};
 static readonly string[] wallNames={"Classique","Météo","Affiche","Aurore","Années 70"};
 readonly List<Slot> slots=new List<Slot>();
 readonly List<View> scene=new List<View>();
 readonly Dictionary<string,CachedArtwork> artworkCache=new Dictionary<string,CachedArtwork>();
 View artworkPreloadRoot;
 int artworkViewsCreated,pageArtworkCold;
 List<LibraryApp> library=new List<LibraryApp>();
 int libraryPage;
 string libraryError="";
 TextLabel worldParis,worldBa;
 TextLabel heroButton;View heroBackdrop;ImageView heroBackdropImage;
 TextLabel clock, weatherPlace, weatherTemperature, weatherCondition, weatherRange, notice, detail;
 TextLabel metroStatus, metroNationTime, metroEtoileTime;
 TextLabel[] tabs;
 static readonly string[] tabNav={"Accueil","Applis","Plex","Maison","Félix","Vélos","Localiser","Énergie","Système","Réglages"};
 static readonly int[] tabPage={0,1,8,5,9,10,11,7,6,2};
 static readonly int[] tabWidth={146,120,96,140,104,116,150,150,158,170};
 // tab pill widths for the glass tab bar (22 px labels)
 static readonly int[] tabPillW={126,110,86,122,98,100,136,124,132,134};
 static readonly string[] haSceneId={"scene.bonjour","scene.bonne_nuit","scene.film","scene.pause"};
 static readonly string[] haSceneName={"Bonjour","Bonne nuit","Film","Pause"};
 static readonly string[] haSceneIcon={"bonjour","bonnenuit","film","pause"};
 static readonly string[] haCoverId={"cover.bureau_bureau","cover.chambre_chambre","cover.salon_salon","cover.cuisine_cuisine"};
 static readonly string[] haCoverName={"Bureau","Chambre","Salon","Cuisine"};
 static readonly string[] haLampId={"light.salon_lampe_entree","light.salon_lampe_musique","light.chambre_lampadaire","light.chambre_lampe_de_chevet_gauche","light.chambre_lampe_de_chevet_droite","light.bureau_lampadaire","light.couloir_couloir","switch.grillplats_plug","light.cuisine","light.cuisine_spots","light.salon_neon_rope_light","light.bureau_lantern_floor_lamp","light.uplighter_floor_lamp","light.tv_backlight_3_lite"};
 static readonly string[] haLampName={"Salon · Entrée","Salon · Musique","Chambre · Lampadaire","Chambre · Gauche","Chambre · Droite","Bureau","Couloir","Lampe Orange","Cuisine","Cuisine · Spots","Salon · Néon","Bureau · Lanterne","Lampadaire Govee","TV · Rétroéclairage"};
 const string cuisineGroup="light.cuisine";
 // Lamp grid on the Maison page: 5 columns of 334 px (14 lamps + the cinema card).
 const int lampCols=5,lampW=334,lampGap=19;
 // Dimmable lamps: OK toggles, a long press on OK opens a brightness panel.
 static readonly HashSet<string> dimLamps=new HashSet<string>{"light.salon_lampe_entree","light.salon_lampe_musique","light.chambre_lampadaire","light.chambre_lampe_de_chevet_gauche","light.chambre_lampe_de_chevet_droite","light.bureau_lampadaire","light.couloir_couloir","light.cuisine"};
 string pendingLamp;Tizen.NUI.Timer longPressTimer;bool keyUpSeen,swallowReturn;
 static readonly HashSet<string> goveeLamps=new HashSet<string>{"light.salon_neon_rope_light","light.bureau_lantern_floor_lamp","light.uplighter_floor_lamp","light.tv_backlight_3_lite","light.cuisine_spots"};
 // Groups driven locally (Matter) whose scenes and music mode go to each bulb through Govee's cloud entities.
 static readonly Dictionary<string,string[]> cloudMembers=new Dictionary<string,string[]>{{"light.cuisine_spots",new[]{"light.smart_led_bulb","light.smart_led_bulb_2","light.smart_led_bulb_3","light.smart_led_bulb_4"}}};
 // The bulbs of a group, switched one by one from its panel (local Matter entities).
 static readonly Dictionary<string,string[]> groupBulbs=new Dictionary<string,string[]>{{"light.cuisine_spots",new[]{"light.h600d","light.h600d_2","light.h600d_4","light.h600d_3"}}};
 static string EntJson(IEnumerable<string> ids){var l=ids.ToList();return l.Count==1?"\""+l[0]+"\"":"["+string.Join(",",l.Select(x=>"\""+x+"\""))+"]";}
 // "light.smart_led_bulb_2" with base "smart_led_bulb" → "select.smart_led_bulb_music_mode_2"
 static IEnumerable<string> MemberEntities(string id,string domain,string suffix){if(!cloudMembers.TryGetValue(id,out var m)){yield return domain+"."+id.Substring(id.IndexOf('.')+1)+suffix;yield break;}string b=m[0].Substring(6);foreach(var x in m)yield return domain+"."+b+suffix+x.Substring(6+b.Length);}
 readonly HashSet<string> colorLights=new HashSet<string>();readonly Dictionary<string,string[]> selectOptions=new Dictionary<string,string[]>();readonly Dictionary<string,int> lightBri=new Dictionary<string,int>();
 readonly Dictionary<string,string> lightEffect=new Dictionary<string,string>();readonly Dictionary<string,string[]> lightEffects=new Dictionary<string,string[]>();
 bool lightOpen;string lightId="";int lightRow,lightCol;
 sealed class GoveeScene { public string Name="",Param="",En=""; public int Code; }
 sealed class GoveeLib { public string Sku="",Ip=""; public List<GoveeScene> Popular=new List<GoveeScene>(); public List<KeyValuePair<string,List<GoveeScene>>> Categories=new List<KeyValuePair<string,List<GoveeScene>>>(); }
 readonly Dictionary<string,GoveeLib> goveeLibs=new Dictionary<string,GoveeLib>();
 bool catalogOpen;int catIndex,catSel,catPage;bool catOnTabs=true;
 const int catCols=6,catRowsPerPage=4;
 readonly Dictionary<string,string> haCoverState=new Dictionary<string,string>();
 readonly Dictionary<string,int> haCoverPos=new Dictionary<string,int>();
 readonly Dictionary<string,DateTime> haCmdAt=new Dictionary<string,DateTime>();
 Tizen.NUI.Timer haRefreshTimer;int haRefreshTicks;
 readonly Dictionary<string,string> haLampState=new Dictionary<string,string>();
 readonly Dictionary<string,View> lampCards=new Dictionary<string,View>();
 readonly Dictionary<string,View> lampDots=new Dictionary<string,View>();
 readonly Dictionary<string,TextLabel> lampNames=new Dictionary<string,TextLabel>();
 readonly Dictionary<string,TextLabel> lampStates=new Dictionary<string,TextLabel>();
 readonly Dictionary<string,TextLabel> coverLabels=new Dictionary<string,TextLabel>();
 bool coverPopupOpen; string coverPopupEntity=""; int coverPopupSel;
 bool pictureOpen;int pictureSel;List<string[]> pictureModes=new List<string[]>();
 readonly List<string[]> felixRooms=new List<string[]>();readonly HashSet<string> cleanRooms=new HashSet<string>();bool cleanOpen;int cleanRow,cleanCol,cleanMode;
 static readonly string[] cleanModeIds={"vacuum","vac_and_mop","mop"},cleanModeNames={"Aspirer","Aspirer et laver","Laver"};
 DateTime lastHaRequest=DateTime.MinValue;
 bool isPaused;
 bool dark,usageDirty;DateTime lastInput=DateTime.Now,lastSensorRequest=DateTime.MinValue;
 View ambientRoot,ambientBlock;TextLabel ambientClock,ambientDate,ambientInfo,ambientIndoor,ambientMetro;Tizen.NUI.Timer ambientTimer;int ambientStep;
 readonly Dictionary<string,int> appUse=new Dictionary<string,int>();readonly Dictionary<string,int> appOrder=new Dictionary<string,int>();
 readonly Dictionary<string,string> haSensor=new Dictionary<string,string>();TextLabel[] homeSensorValue;
 static readonly Color inkFixed=new Color(0.114f,0.114f,0.122f,1);
 const string haSensorWanted="sensor.salon_sensor_temperature,sensor.salon_sensor_humidite,sensor.chambre_temperature_temperature,sensor.chambre_temperature_humidite,sensor.linky_*_dernier_releve,vacuum.felix,sensor.felix_etat,sensor.felix_batterie,sensor.felix_fin_du_dernier_nettoyage,sensor.felix_surface_de_nettoyage,"+cinemaAuto;
 static readonly string[] frDays={"dimanche","lundi","mardi","mercredi","jeudi","vendredi","samedi"};
 static readonly string[] frMonths={"janvier","février","mars","avril","mai","juin","juillet","août","septembre","octobre","novembre","décembre"};
 readonly Dictionary<string,int> lineStatus=new Dictionary<string,int>();
 readonly Dictionary<string,string> lineHeader=new Dictionary<string,string>();
 readonly Dictionary<string,string> lineDesc=new Dictionary<string,string>();
 bool lineDetailOpen; string lineDetailKey="";
 DateTime lastLinesRequest=DateTime.MinValue;
 DateTime linesUpdated=DateTime.MinValue;
 Tizen.NUI.Timer systemTimer;DateTime lastNetScan=DateTime.MinValue;
 string guardStatus="Vérification en cours…";DateTime lastGuard=DateTime.MinValue;bool guardOk;
 string sysBuild="—",sysOs="—",sysHost="—",sysKernel="—",sysUptime="—";
 string sysIp="—",sysMac="—",sysIface="—",sysCpuModel="—",sysCpuPct="…",sysLoad="—";
 int sysCpuCores=0;long memTotalKb=0,memUsedKb=0;double memPct=0;ulong lastCpuTotal=0,lastCpuIdle=0;
 readonly List<string[]> sysParts=new List<string[]>();
 TextLabel sysCpuVal,sysLoadVal,sysMemVal,sysUptimeVal;View sysCpuFill,sysMemFill;
 static readonly string[] metroCodes={"1","2","3","3bis","4","5","6","7","7bis","8","9","10","11","12","13","14"};
 static readonly string[] metroHex={"#FFCE00","#0064B0","#9F971A","#98D4E2","#C04191","#F28E42","#83C491","#F3A4BA","#83C491","#CEABD9","#D5C900","#E3B32A","#8D5E2A","#00814F","#98D4E2","#662483"};
 static readonly string[] rerCodes={"A","B","C","D","E"};
 static readonly string[] rerHex={"#F7403A","#4D97CC","#F3D311","#5E9620","#B90845"};
 TextLabel newsTopTitle,marketHeadline,factBody;
 View weatherCard, weatherIcon, metroCard;
 int weatherSlotIndex=-1,metroSlotIndex=-1,marketSlotIndex=-1,factSlotIndex=-1,newsButtonSlotIndex=-1,newsCategorySlotStart=-1,newsArticleSlotStart=-1;
 readonly List<ForecastDay> forecast=new List<ForecastDay>();
 readonly List<ForecastHour> forecastHours=new List<ForecastHour>();
 readonly List<MetroDeparture> metroDepartures=new List<MetroDeparture>();
 readonly List<View> hourCards=new List<View>();
 readonly List<View> dayCards=new List<View>();
 readonly List<View> weatherGlyphs=new List<View>();
 readonly List<Animation> weatherAnimations=new List<Animation>();
 readonly List<KeyFrames> weatherKeyFrames=new List<KeyFrames>();
 readonly List<Animation> rainAnimations=new List<Animation>();
 readonly List<KeyFrames> rainKeyFrames=new List<KeyFrames>();
 TextLabel toolbarHint;
 Animation focusAnimation;
 bool fontLoaded;
 Tizen.NUI.Timer clockTimer;
 DateTime lastWeatherRequest=DateTime.MinValue;
 DateTime lastMetroRequest=DateTime.MinValue;
 DateTime lastTrafficRequest=DateTime.MinValue;
 DateTime lastNewsRequest=DateTime.MinValue,lastFactUpdate=DateTime.MinValue;
 string metroUpdated="",metroSource="";
 bool metroAvailable;
 string trafficUpdated="",trafficHeader="",trafficDescription="",trafficEffect="";
 bool trafficAvailable,trafficHasAlert;
 string newsUpdated="";
 readonly List<NewsSection> newsSections=new List<NewsSection>();
 readonly string[] newsSectionNames={"Paris","France","Monde","Bourse"};
 readonly List<MarketQuote> marketQuotes=new List<MarketQuote>();
 readonly Dictionary<string,ArticleFull> articleCache=new Dictionary<string,ArticleFull>();
 DateTime lastMarketsRequest=DateTime.MinValue,articlePendingAt=DateTime.MinValue;string articlePending="";
 TextLabel marketValue,marketChange,marketTime,marketSub;View marketChartHost;
 // French number format forced by hand: on this TV the fr-FR culture data is not always available (prices came out as 8,352.00).
 static readonly CultureInfo frCulture=FrenchNumbers();
 static CultureInfo FrenchNumbers(){var c=(CultureInfo)CultureInfo.GetCultureInfo("fr-FR").Clone();var n=c.NumberFormat;n.NumberDecimalSeparator=",";n.NumberGroupSeparator="\u202F";n.PercentDecimalSeparator=",";n.CurrencyDecimalSeparator=",";n.NegativeSign="-";return c;}
 const string cinemaAuto="automation.belair_mode_cinema_plex";
 TextLabel cinemaState,felixState;View cinemaCard,cinemaDot;
 readonly List<EnergyDay> energyDays=new List<EnergyDay>();DateTime lastEnergyRequest=DateTime.MinValue;
 List<string> facts;DateTime lastFactsRequest=DateTime.MinValue;
 readonly string[] randomFacts={
  "Paris compte 20 arrondissements. Le 12e s’étend de la Bastille au bois de Vincennes, l’un des plus grands espaces verts de la capitale.",
  "Une pieuvre a trois cœurs : deux envoient le sang vers les branchies et le troisième l’envoie dans le reste du corps.",
  "La tour Eiffel peut grandir d’une quinzaine de centimètres en été. La chaleur dilate le métal, puis elle reprend sa taille quand il refroidit.",
  "Sur Vénus, un jour solaire dure environ 243 jours terrestres, alors que son année ne dure que 225 jours terrestres.",
  "Les abeilles indiquent une source de nourriture par une danse. L’angle et la durée de leurs mouvements renseignent les autres sur la direction et la distance.",
  "La Lune s’éloigne de la Terre d’environ 3,8 cm par an. Les marées transfèrent lentement une partie de l’énergie de la rotation terrestre vers son orbite.",
  "Le cœur d’une baleine bleue peut peser près de 180 kg. Son pouls ralentit fortement lorsqu’elle plonge en profondeur.",
  "Le Louvre était une forteresse bâtie à la fin du XIIe siècle. Des vestiges de ses fondations sont encore visibles sous le musée.",
  "La lumière du Soleil met environ 8 minutes et 20 secondes à atteindre la Terre. La voir, c’est observer le Soleil tel qu’il était quelques minutes plus tôt.",
  "Le mont Everest gagne quelques millimètres par an sous l’effet des mouvements des plaques tectoniques, même si l’érosion agit en sens inverse.",
  "Les flamants roses tirent leur couleur des pigments présents dans les algues et petits crustacés qu’ils mangent.",
  "Le plus petit os du corps humain est l’étrier, dans l’oreille moyenne. Il mesure seulement quelques millimètres et transmet les vibrations du son.",
  "La Station spatiale internationale fait le tour de la Terre en environ 90 minutes. Son équipage peut ainsi voir jusqu’à 16 levers de Soleil par jour."
 };
 int newsCategoryIndex,newsArticleIndex,newsDetailPage,newsReturnSelected,newsReturnArea,factIndex;
 bool newsMenuOpen,newsDetailOpen,factDetailOpen,quoteDetailOpen;int quoteIndex,quoteSlotStart=-1,quoteRange;
 static readonly string[] frMonthShort={"janv.","févr.","mars","avr.","mai","juin","juil.","août","sept.","oct.","nov.","déc."};
 static readonly string[] rangeIds={"1d","5d","1mo","6mo","1y","max"},rangeNames={"Jour","Semaine","Mois","6 mois","Année","Max"},rangePeriod={"aujourd’hui","sur 5 jours","sur 1 mois","sur 6 mois","sur 1 an","depuis l’origine"},rangeStep={"5 minutes","30 minutes","heure","jour","jour","mois"};
 readonly Dictionary<string,MarketQuote> seriesCache=new Dictionary<string,MarketQuote>();readonly Dictionary<string,DateTime> seriesAt=new Dictionary<string,DateTime>();
 string weatherText="Paris 12e · chargement…", weatherTemperatureText="--°", weatherConditionText="Chargement…", weatherHigh="", weatherLow="";
 int weatherCode=-1;
 int forecastHourOffset,forecastHourFocus,forecastDayFocus,selectedForecastHour=-1,selectedForecastDay=-1;
 bool forecastDetailOpen;
 int page, selected, area=1, lastSource=13;
 string editing="";
 long buildMilliseconds;
 int currentVolume, currentMute;
 readonly Dictionary<string,string> settingValues=new Dictionary<string,string>();
 readonly Dictionary<string,string> settingNames=new Dictionary<string,string>();
 readonly Dictionary<string,bool> settingEnabled=new Dictionary<string,bool>();
 readonly Dictionary<string,double[]> levelRange=new Dictionary<string,double[]>();
 string lastKey="";
 long lastKeyAt;
 Color ink=new Color(0.114f,0.114f,0.122f,1);
 Color muted=new Color(0.431f,0.431f,0.451f,1);
 readonly Color white=new Color(1,1,1,1);
 Color accent=new Color(0.24f,0.29f,0.72f,1);
 [DllImport("libsource-api.so")] static extern int connect_source(int sourceType);
 [DllImport("libvconf.so.0")] static extern int vconf_get_int(string key,out int value);
 [DllImport("libvconf.so.0")] static extern int vconf_set_int(string key,int value);
 [DllImport("libvconf.so.0")] static extern int vconf_get_bool(string key,out int value);
 [DllImport("libvconf.so.0")] static extern int vconf_set_bool(string key,int value);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_create(out IntPtr app);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_set_app_id(IntPtr app,string id);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_set_operation(IntPtr app,string operation);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_set_uri(IntPtr app,string uri);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_add_extra_data(IntPtr app,string key,string value);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_send_launch_request(IntPtr app,IntPtr cb,IntPtr data);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_destroy(IntPtr app);
 [DllImport("libcapi-media-sound-manager-tv.so.0")] static extern int sound_manager_get_master_volume(out int value);
 [DllImport("libcapi-media-sound-manager-tv.so.0")] static extern int sound_manager_set_master_volume(int value);
 [DllImport("libcapi-media-sound-manager-tv.so.0")] static extern int sound_manager_get_master_mute(out int value);
 [DllImport("libcapi-media-sound-manager-tv.so.0")] static extern int sound_manager_set_master_mute(int value);
 [DllImport("libsettings-api.so")] static extern bool CSharpGetItemValue(string id,out string name,out string value);
 [DllImport("libsettings-api.so")] static extern bool CSharpSetItemValue(string id,string value);
 [DllImport("libsettings-api.so")] static extern bool CSharpGetItemEnable(string id,out bool enabled);
 [DllImport("libsettings-api.so")] static extern bool CSharpGetItemMinMaxStepValue(string id,out double min,out double max,out double step);
 [DllImport("libsettings-api.so")] static extern bool CSharpGetItemDataList(string id,System.Text.StringBuilder list);
 [DllImport("libsettings-api.so")] [return:MarshalAs(UnmanagedType.U1)] static extern bool CSharpGetItemDataListEnable(string id,string value,[MarshalAs(UnmanagedType.U1)] out bool enabled);



 protected override void OnCreate()
 {
  base.OnCreate();
  try{fontLoaded=FontClient.Instance.AddCustomFontDirectory(Root+"fonts");}catch{fontLoaded=false;}
  try{library=AppLibrary.Load(Root+"apps.json");}catch(Exception ex){libraryError=ex.Message;}
  LoadUsage();LoadPrefs();ApplyTheme();LoadGovee();UpdateGuard();
  Window.Default.Title="Quiet Home";Window.Default.KeyEvent+=OnKey;
  PreloadArtwork();
  facts=new List<string>(randomFacts);factIndex=new Random(Environment.TickCount).Next(facts.Count);lastFactUpdate=DateTime.Now;
  RefreshSource();selected=SourceIndex();BuildPage();
  clockTimer=new Tizen.NUI.Timer(30000);
  clockTimer.Tick+=(s,e)=>{if(clock!=null)clock.Text=DateTime.Now.ToString("HH:mm");UpdateWorldClock();if(DateTime.Now-lastGuard>=TimeSpan.FromMinutes(10))UpdateGuard();if(briefClock!=null)briefClock.Text=DateTime.Now.ToString("HH:mm");if(WantDark()!=dark){ApplyTheme();BuildPage();}else if(prefWall==1&&WallKind()!=wallKind&&editing==""&&!coverPopupOpen)BuildPage();if(!isPaused&&ambientRoot==null&&briefRoot==null&&editing==""&&(DateTime.Now-lastInput).TotalSeconds>=AmbientDelay())OpenAmbient();RequestDue();UpdateHero();if(page==0&&!factDetailOpen&&DateTime.Now-lastFactUpdate>=TimeSpan.FromMinutes(15))RotateFact();return true;};clockTimer.Start();RequestHomeData();lastSensorRequest=DateTime.Now;RequestHaStates();MaybeBrief();SendWatch();WriteState("created");
 }
 protected override void OnResume(){base.OnResume();isPaused=false;lastInput=DateTime.Now;clockTimer?.Start();UpdateGuard();if(tabs!=null&&WantDark()!=dark){ApplyTheme();BuildPage();}bool themeDue=WantDark()!=dark;if(themeDue)ApplyTheme();bool rebuild=(themeDue||usageDirty&&(page==0||page==1))&&tabs!=null;usageDirty=false;if(rebuild)BuildPage();if(page==6)StartSystemTimer();RefreshSource();if(tabs!=null&&page==2){RefreshSettings();UpdateFocus(false);}RequestDue();MaybeBrief();SendWatch();WriteState("resumed");}
 protected override void OnAppControlReceived(AppControlReceivedEventArgs e)
 {
  base.OnAppControlReceived(e);
  string requested="";
  e.ReceivedAppControl.ExtraData.TryGet("quiet_page",out requested);
  if(requested!=null&&requested.StartsWith("show:")){lastInput=DateTime.Now;CloseAmbient();CloseOverlays();}
  if(requested=="settings")OpenSettings("settings_request");
  else if(requested=="apps"){page=1;area=1;selected=0;BuildPage();}
  else if(requested!=null&&requested.StartsWith("show:news:")){ // test entry point: show:news:<section>[:<article>[:<page>]]
   var parts=requested.Split(':');int c,i,pg;
   if(int.TryParse(parts[2],out c)){page=0;area=1;BuildPage();OpenNewsMenu(c);
    if(parts.Length>3&&int.TryParse(parts[3],out i)&&i<Math.Min(ArticleLimit(),NewsItems(newsCategoryIndex).Count)){
     newsArticleIndex=i;newsDetailPage=parts.Length>4&&int.TryParse(parts[4],out pg)?pg:0;newsDetailOpen=true;area=9;RequestArticle(SelectedNewsArticle());BuildPage();UpdateFocus(false);}}
  }
  else if(requested=="show:brief"){page=0;area=1;selected=0;BuildPage();OpenBrief();}
  else if(requested!=null&&requested.StartsWith("show:quote:")){var qp=requested.Substring(11).Split(':');int qi,qr=0;if(int.TryParse(qp[0],out qi)){if(qp.Length>1)int.TryParse(qp[1],out qr);page=0;area=1;BuildPage();OpenNewsMenu(3);quoteIndex=qi;quoteRange=Math.Max(0,Math.Min(5,qr));area=11;quoteDetailOpen=true;BuildPage();}}
  else if(requested!=null&&requested.StartsWith("show:light:")){page=5;area=1;selected=0;lightId=requested.Substring(11);lightOpen=true;lightRow=0;lightCol=0;BuildPage();}
  else if(requested!=null&&requested.StartsWith("show:catalog:")){page=5;area=1;selected=0;lightId=requested.Substring(13);lightOpen=true;catalogOpen=true;catIndex=0;catSel=0;catPage=0;catOnTabs=true;BuildPage();}
  else if(requested!=null&&requested.StartsWith("show:cover:")){page=5;area=1;selected=0;coverPopupEntity=requested.Substring(11);coverPopupSel=2;coverPopupOpen=true;BuildPage();}
  else if(requested!=null&&requested.StartsWith("show:findfocus:")&&Demo()){page=11;area=1;findFocus="device_tracker.demo_"+requested.Substring(15);BuildPage();}
  else if(requested=="show:infos"){page=0;homeShelf=1;area=4;selected=0;BuildPage();var r=Row(-2);if(r.Count>0)selected=r[0];UpdateFocus(false);}
  else if(requested=="show:herodemo"){page=0;area=1;selected=0;CloseOverlays();heroDemo=true;if(plexItems.Count==0)RequestExtra(true,true,false);else ApplyHeroDemo();}
  else if(requested!=null&&requested.StartsWith("show:pref:")){var pp=requested.Substring(10).Split(':');int pv;if(pp.Length==2&&int.TryParse(pp[1],out pv)){if(pp[0]=="wall")prefWall=Math.Max(0,Math.Min(wallNames.Length-1,pv));else if(pp[0]=="accent")prefAccent=Math.Max(0,Math.Min(accentNames.Length-1,pv));SavePrefs();ApplyTheme();BuildPage();}}
  else if(requested=="show:appearance"){page=2;area=1;CloseOverlays();appearOpen=true;appearRow=1;appearCol=prefWall;BuildPage();}
  else if(requested=="show:close"){CloseBrief(false);pictureOpen=false;cleanOpen=false;BuildPage();}
  else if(requested=="show:picture"){page=2;area=1;selected=3;BuildPage();OpenPicture();}
  else if(requested=="show:clean"){page=9;area=1;selected=0;cleanOpen=true;cleanRow=0;cleanCol=0;BuildPage();}
  else if(requested!=null&&requested.StartsWith("show:")){CloseBrief(false);int n;if(int.TryParse(requested.Substring(5),out n)&&n>=0&&n<=11){page=n;homeShelf=0;area=1;selected=0;BuildPage();}}
  else if(requested=="extra"){
   string value;
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_media",out value))ReceiveMedia(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_felix",out value))ReceiveFelix(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_series",out value))ReceiveSeries(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_facts",out value))ReceiveFacts(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_mobility",out value))ReceiveMobility(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_rain",out value))ReceiveRain(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_basemap",out value))ReceiveBasemap(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_find",out value))ReceiveFind(value);
  }
  else if(requested=="weather"||requested=="transit"||requested=="traffic"||requested=="data"||requested=="news"){
   string value;
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_temperature",out value))ReceiveWeather(e);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_metro",out value))ReceiveMetro(e);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_traffic",out value))ReceiveTraffic(e);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_news",out value))ReceiveNews(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_lines",out value))ReceiveLines(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_markets",out value))ReceiveMarkets(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_energy",out value))ReceiveEnergy(value);
   UpdateBrief();
  }else if(requested=="article"){
   string value;if(e.ReceivedAppControl.ExtraData.TryGet("quiet_article",out value))ReceiveArticle(value);
  }else if(requested=="ha"){
   string value;HTlog("reply ha data="+e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_data",out value));
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_data",out value))ReceiveHaData(value);
   if(e.ReceivedAppControl.ExtraData.TryGet("quiet_ha_result",out value)&&value=="fail"){ShowError("Commande maison échouée.");haCmdAt.Clear();RequestHaStates();}
   UpdateBrief();
  }else Home("home_request");
 }
 protected override void OnPause(){base.OnPause();isPaused=true;appearOpen=false;CloseAmbient();CloseBrief(false);pictureOpen=false;cleanOpen=false;lightOpen=false;catalogOpen=false;editing="";clockTimer?.Stop();systemTimer?.Stop();haRefreshTimer?.Stop();findUnlocked=false;pinEntry="";SendWatch();WriteState("paused");}
 void CloseOverlays(){appearOpen=false;newsMenuOpen=false;newsDetailOpen=false;quoteDetailOpen=false;factDetailOpen=false;forecastDetailOpen=false;lineDetailOpen=false;coverPopupOpen=false;lightOpen=false;catalogOpen=false;pictureOpen=false;cleanOpen=false;editing="";}
 void Home(string activity){clockTimer?.Start();RefreshSource();if(tabs!=null){bool unchanged=page==0&&area==1&&editing==""&&selected==SourceIndex()&&!newsMenuOpen&&!factDetailOpen&&!lightOpen&&!coverPopupOpen&&!pictureOpen&&!cleanOpen;page=0;area=1;CloseOverlays();selectedForecastHour=-1;selectedForecastDay=-1;selected=SourceIndex();if(!unchanged)BuildPage();RequestDue();MaybeBrief();WriteState(activity);}}
 void OpenSettings(string activity){clockTimer?.Start();page=2;area=1;selected=0;editing="";newsMenuOpen=false;newsDetailOpen=false;factDetailOpen=false;forecastDetailOpen=false;selectedForecastHour=-1;selectedForecastDay=-1;BuildPage();WriteState(activity);}
 void RefreshSource(){try{int n;if(vconf_get_int("memory/system/source_type",out n)==0&&n==13)lastSource=n;}catch{}}
 int SourceIndex(){return 0;}
 View Add(View v){scene.Add(v);Window.Default.Add(v);return v;}
 View Panel(int x,int y,int w,int h,Color color,int radius=18){return Add(new View{Position2D=new Position2D(x,y),Size2D=new Size2D(w,h),BackgroundColor=color,CornerRadius=new Vector4(radius,radius,radius,radius)});}
 TextLabel Text(string text,int x,int y,int w,int h,float size,Color color,string face="Medium")
 {return (TextLabel)Add(Label(text,x,y,w,h,size,color,face));}
 TextLabel Label(string text,int x,int y,int w,int h,float size,Color color,string face="Medium")
 {return new TextLabel{Text=text,Position2D=new Position2D(x,y),Size2D=new Size2D(w,h),PixelSize=size,FontFamily="Quiet Inter "+face,TextColor=color,VerticalAlignment=VerticalAlignment.Center,Ellipsis=false};}
 // ---- Accueil "Infos" row: five glass tiles of one size ----
 int homeShelf;TextLabel ihTop,ihBig,ihDetail,ihLine;
 void BuildInfoHero()
 {
  ihTop=Text("",96,234,1400,34,22,muted,"Semibold");
  ihBig=Text("",88,262,1500,160,150,ink,"Display");ihBig.Ellipsis=false;
  ihDetail=Text("",96,420,1500,46,34,ink,"Medium");
  ihLine=Text("",96,470,1500,40,26,muted,"Medium");
 }
 void UpdateInfoHero()
 {
  if(ihTop==null||page!=0||homeShelf!=1||selected<0||selected>=slots.Count)return;
  ihBig.MultiLine=false;ihBig.PixelSize=150;ihBig.Size2D=new Size2D(1500,160);ihDetail.Position2D=new Position2D(96,420);ihLine.Position2D=new Position2D(96,470);
  if(selected==weatherSlotIndex){ihTop.Text="MÉTÉO · PARIS 12E";ihBig.Text=weatherTemperatureText;ihDetail.Text=OneLine(weatherConditionText)+" · "+WeatherRange();ihLine.Text=RainText()+" · OK pour les prévisions sur 7 jours";}
  else if(selected==metroSlotIndex){ihTop.Text="MÉTRO 6 · BEL-AIR";ihBig.Text=MetroNextText("Nation");ihDetail.Text="vers Nation · "+MetroNextText("Etoile")+" vers Charles-de-Gaulle–Étoile";ihLine.Text=(trafficHasAlert?"Trafic perturbé":trafficAvailable?"Trafic normal":"Trafic inconnu")+" · OK pour le trafic et les prochains passages";}
  else if(selected==marketSlotIndex){ihTop.Text="BOURSE · CAC 40";ihBig.Text=marketValue?.Text??"";ihDetail.Text=marketChange?.Text??"";ihLine.Text=(marketSub?.Text??"")+" · OK pour les cours";}
  else if(selected==factSlotIndex){ihTop.Text="LE SAVIEZ-VOUS ?";ihBig.Text=facts[factIndex];ihBig.MultiLine=true;ihBig.LineWrapMode=LineWrapMode.Word;ihBig.PixelSize=40;ihBig.Size2D=new Size2D(1400,190);ihDetail.Text="";ihLine.Position2D=new Position2D(96,470);ihLine.Text="OK pour lire en entier";}
  else{ihTop.Text="MAISON";ihBig.Text=SensorNum("sensor.salon_sensor_temperature","0.0")+"°";ihDetail.Text="Salon "+(homeSensorValue!=null?homeSensorValue[0].Text:"")+" · Chambre "+(homeSensorValue!=null?homeSensorValue[1].Text:"");ihLine.Text="Électricité "+(homeSensorValue!=null?homeSensorValue[2].Text:"")+" · OK pour la page Maison";}
 }
 // Maison shelf on the Accueil: the four scenes, one press away
 void BuildHomeShelf()
 {
  int g=24,w=(1728-4*g)/5,h=120,y=852;
  for(int i=0;i<haSceneId.Length;i++){
   int x=96+i*(w+g);
   var card=Panel(x+w/2,y+h/2,w,h,GlassFill(),26);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;card.BorderlineWidth=1;card.BorderlineColor=GlassEdge();
   card.Add(new ImageView{ResourceUrl=IconFile(sceneIcon2[i],dark?sceneIconDark[i]:sceneTint[i]),Position2D=new Position2D(28,(h-46)/2),Size2D=new Size2D(46,46)});
   card.Add(Label(haSceneName[i],96,24,w-116,40,28,ink,"Semibold"));
   card.Add(Label(sceneHint[i],96,64,w-116,32,20,muted,"Medium"));
   slots.Add(new Slot{Card=card,Name="Scène "+haSceneName[i],Action="ha_scene:"+haSceneId[i],Description="OK pour activer la scène",Row=-3,X=x,Y=y,W=w,H=h});
  }
  // shortcut: the TV backlight, OK opens its light sheet (brightness, scenes, music, options) over the Accueil
  {
   int x=96+4*(w+g);bool act=IsLampOn(backlightId);
   var card=Panel(x+w/2,y+h/2,w,h,act?new Color(accent.R,accent.G,accent.B,dark?.24f:.16f):GlassFill(),26);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;card.BorderlineWidth=1;card.BorderlineColor=GlassEdge();
   card.Add(new ImageView{ResourceUrl=IconFile("tv",dark?"#9FC3FF":"#2F6BFF"),Position2D=new Position2D(28,(h-46)/2),Size2D=new Size2D(46,46)});
   card.Add(Label("Rétroéclairage",96,24,w-116,40,28,ink,"Semibold"));
   backlightHint=Label(LampStateText(backlightId),96,64,w-116,32,20,act?accent:muted,"Medium");card.Add(backlightHint);
   slots.Add(new Slot{Card=card,Name="Rétroéclairage TV",Action="light:"+backlightId,Description="OK pour la luminosité, les scènes et les options",Row=-3,X=x,Y=y,W=w,H=h});
  }
 }
 const string backlightId="light.tv_backlight_3_lite";TextLabel backlightHint;
 const int infoY=588,infoW=326,infoH=184,infoGap=24;
 static int InfoX(int i){return 96+i*(infoW+infoGap);}
 View InfoTile(int i){var t=Panel(InfoX(i)+infoW/2,infoY+infoH/2,infoW,infoH,GlassFill(),28);t.PivotPoint=new Position(.5f,.5f,.5f);t.PositionUsesPivotPoint=true;t.BorderlineWidth=1;t.BorderlineColor=GlassEdge();return t;}
 void BuildWeatherWidget()
 {
  int x=InfoX(0),y=infoY,w=infoW,h=infoH;
  weatherCard=InfoTile(0);
  weatherPlace=Label("MÉTÉO",22,14,200,28,18,muted,"Semibold");weatherCard.Add(weatherPlace);
  weatherIcon=new View{Position2D=new Position2D(w-102,6),Size2D=new Size2D(88,88)};weatherCard.Add(weatherIcon);
  weatherTemperature=Label(weatherTemperatureText,18,40,200,78,66,ink,"Display");weatherTemperature.Ellipsis=false;weatherCard.Add(weatherTemperature);
  weatherCondition=Label(OneLine(weatherConditionText),22,116,w-44,28,21,ink,"Semibold");weatherCondition.MultiLine=false;weatherCard.Add(weatherCondition);
  weatherRange=Label(WeatherRange(),22,144,w-44,26,19,muted,"Medium");weatherCard.Add(weatherRange);
  weatherRain=Label(RainText(),22,144,w-44,26,19,RainColor(),"Semibold");weatherRain.Opacity=0;weatherCard.Add(weatherRain);
  DrawWeatherIcon();AnimateWeatherIcon(weatherIcon,1.2f);ShowRainLine();
  weatherSlotIndex=slots.Count;
  slots.Add(new Slot{Card=weatherCard,Label=weatherPlace,Name="Météo Paris 12e",Action="weather",Description=RainText()+" · OK pour les prévisions",Row=-2,X=x,Y=y,W=w,H=h});
 }
 void BuildMetroWidget()
 {
  int x=InfoX(1),y=infoY,w=infoW,h=infoH;
  metroCard=InfoTile(1);
  metroCard.Add(Label("MÉTRO 6",22,14,150,28,18,muted,"Semibold"));
  metroStatus=Label(MetroStatusText(),150,14,w-172,28,17,TrafficColor(),"Semibold");metroStatus.HorizontalAlignment=HorizontalAlignment.End;metroCard.Add(metroStatus);
  metroNationTime=AddMetroTileDirection("Nation","Nation",52);
  metroEtoileTime=AddMetroTileDirection("Étoile","Etoile",104);
  metroFoot=null;
  metroSlotIndex=slots.Count;
  slots.Add(new Slot{Card=metroCard,Label=metroStatus,Name="Métro 6 · Bel-Air",Action="metro",Description="Bel-Air · OK pour le trafic et les prochains passages",Row=-2,X=x,Y=y,W=w,H=h});
 }
 TextLabel AddMetroTileDirection(string title,string direction,int y)
 {
  var row=new View{Position2D=new Position2D(14,y),Size2D=new Size2D(infoW-28,46),BackgroundColor=dark?new Color(1,1,1,.07f):new Color(0,0,0,.04f),CornerRadius=new Vector4(14,14,14,14)};metroCard.Add(row);
  var lineNumber=new View{Position2D=new Position2D(8,7),Size2D=new Size2D(32,32),BackgroundColor=new Color(.36f,.73f,.49f,1),CornerRadius=new Vector4(16,16,16,16)};row.Add(lineNumber);
  var number=Label("6",0,0,32,32,18,white,"Semibold");number.HorizontalAlignment=HorizontalAlignment.Center;lineNumber.Add(number);
  row.Add(Label(title,50,0,110,46,20,ink,"Semibold"));
  var arrival=Label(MetroNextText(direction),150,0,infoW-28-160,46,20,ink,"Semibold");arrival.HorizontalAlignment=HorizontalAlignment.End;row.Add(arrival);return arrival;
 }
 string MetroStatusText(){return !trafficAvailable?"Trafic ?":trafficHasAlert?"PERTURBÉ":"TRAFIC OK";}
 Color TrafficColor(){return trafficHasAlert?(dark?new Color(1f,.55f,.50f,1):new Color(.72f,.16f,.13f,1)):trafficAvailable?(dark?new Color(.45f,.86f,.60f,1):new Color(.13f,.48f,.30f,1)):muted;}
 string MetroUpdatedTime(){DateTime time;return DateTime.TryParse(metroUpdated,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out time)?time.ToLocalTime().ToString("HH:mm"):"—";}
 string TrafficUpdatedTime(){DateTime time;return DateTime.TryParse(trafficUpdated,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out time)?time.ToLocalTime().ToString("HH:mm"):"—";}
 MetroDeparture NextMetro(string direction){foreach(var departure in metroDepartures)if(departure.Destination.IndexOf(direction,StringComparison.OrdinalIgnoreCase)>=0)return departure;return null;}
 string MetroNextText(string direction){var departure=NextMetro(direction);return departure==null?"—":departure.AtStop?"À quai":departure.Minutes==0?"<1 min":departure.Minutes+" min";}
 void UpdateMetroWidget(){if(metroStatus==null)return;metroStatus.Text=MetroStatusText();metroStatus.TextColor=TrafficColor();metroNationTime.Text=MetroNextText("Nation");metroEtoileTime.Text=MetroNextText("Etoile");}
 string WeatherRange(){return "Max "+(weatherHigh==""?"--°":weatherHigh)+" · Min "+(weatherLow==""?"--°":weatherLow);}
 void ReceiveWeather(AppControlReceivedEventArgs e)
 {
  string value;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_temperature",out value))weatherTemperatureText=value;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_condition",out value))weatherConditionText=value;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_high",out value))weatherHigh=value;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_low",out value))weatherLow=value;
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_weather_code",out value))int.TryParse(value,out weatherCode);
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_forecast",out value))ReadForecast(value);
  if(e.ReceivedAppControl.ExtraData.TryGet("quiet_hours",out value))ReadForecastHours(value);
  weatherText="Paris 12e · "+weatherTemperatureText+" · "+weatherConditionText;
  if(weatherHigh!=""&&weatherLow!="")weatherText+=" · max "+weatherHigh+" / min "+weatherLow;
  if(weatherTemperature!=null){weatherTemperature.Text=weatherTemperatureText;weatherCondition.Text=OneLine(weatherConditionText);weatherRange.Text=WeatherRange();DrawWeatherIcon();}
  if(page==3)BuildPage();
  WriteState("weather_received");
 }
 void ReceiveMetro(AppControlReceivedEventArgs e)
 {
  string json;if(!e.ReceivedAppControl.ExtraData.TryGet("quiet_metro",out json))return;
  metroDepartures.Clear();metroAvailable=false;metroUpdated="";metroSource="";
  try{
   using(var data=JsonDocument.Parse(json)){
    var root=data.RootElement;
    metroAvailable=root.TryGetProperty("Available",out var available)&&available.GetBoolean();
    if(root.TryGetProperty("Updated",out var updated))metroUpdated=updated.GetString()??"";
    if(root.TryGetProperty("Source",out var source))metroSource=source.GetString()??"";
    if(root.TryGetProperty("Next",out var next)&&next.ValueKind==JsonValueKind.Array)foreach(var item in next.EnumerateArray()){
     metroDepartures.Add(new MetroDeparture{
      Destination=item.TryGetProperty("Destination",out var destination)?destination.GetString()??"":"",
      Time=item.TryGetProperty("Time",out var time)?time.GetString()??"":"",
      Minutes=item.TryGetProperty("Minutes",out var minutes)?minutes.GetInt32():0,
      Realtime=item.TryGetProperty("Realtime",out var realtime)&&realtime.GetBoolean(),
      AtStop=item.TryGetProperty("AtStop",out var atStop)&&atStop.GetBoolean()
     });
    }
   }
  }catch{metroDepartures.Clear();metroAvailable=false;}
  UpdateMetroWidget();if(page==4&&!lineDetailOpen)BuildPage();
  WriteState("metro_received:"+metroAvailable+":"+metroDepartures.Count);
 }
 void ReceiveTraffic(AppControlReceivedEventArgs e)
 {
  string json;if(!e.ReceivedAppControl.ExtraData.TryGet("quiet_traffic",out json))return;
  trafficAvailable=false;trafficHasAlert=false;trafficUpdated="";trafficHeader="";trafficDescription="";trafficEffect="";
  try{
   using(var data=JsonDocument.Parse(json)){
    var root=data.RootElement;
    trafficAvailable=root.TryGetProperty("Available",out var available)&&available.GetBoolean();
    trafficHasAlert=root.TryGetProperty("HasAlert",out var hasAlert)&&hasAlert.GetBoolean();
    if(root.TryGetProperty("Updated",out var updated))trafficUpdated=updated.GetString()??"";
    if(root.TryGetProperty("Header",out var header))trafficHeader=header.GetString()??"";
    if(root.TryGetProperty("Description",out var description))trafficDescription=description.GetString()??"";
    if(root.TryGetProperty("Effect",out var effect))trafficEffect=effect.GetString()??"";
   }
  }catch{trafficAvailable=false;trafficHasAlert=false;}
  UpdateMetroWidget();
  WriteState("traffic_received:"+trafficAvailable+":"+trafficHasAlert);
 }
 void ReceiveNews(string json)
 {
  try{
   using(var data=JsonDocument.Parse(json)){
    var root=data.RootElement;
    newsUpdated=root.TryGetProperty("Updated",out var updated)?updated.GetString()??"":"";
    newsSections.Clear();
    if(root.TryGetProperty("Sections",out var sections)&&sections.ValueKind==JsonValueKind.Array){
     foreach(var section in sections.EnumerateArray()){
      var parsedSection=new NewsSection{Name=section.TryGetProperty("Name",out var name)?name.GetString()??"":""};
      if(section.TryGetProperty("Items",out var items)&&items.ValueKind==JsonValueKind.Array){
       foreach(var item in items.EnumerateArray()){
        parsedSection.Items.Add(new NewsArticle{
         Title=item.TryGetProperty("Title",out var title)?title.GetString()??"":"",
         Summary=item.TryGetProperty("Summary",out var summary)?summary.GetString()??"":"",
         Url=item.TryGetProperty("Url",out var url)?url.GetString()??"":"",
         Published=item.TryGetProperty("Published",out var published)?published.GetString()??"":"",
         Source=item.TryGetProperty("Source",out var source)?source.GetString()??"":"",
         Image=item.TryGetProperty("Image",out var image)?image.GetString()??"":""
        });
       }
      }
      newsSections.Add(parsedSection);
     }
    }
   }
  }catch{newsSections.Clear();newsUpdated="";}
  if(newsTopTitle!=null)newsTopTitle.Text=NewsButtonText();
  if(page==0&&newsMenuOpen){
   int previousArea=area;
   BuildPage();
   if(newsDetailOpen)area=9;
   else if(previousArea==7)selected=newsCategorySlotStart+newsCategoryIndex;
   else if(previousArea==8&&newsArticleSlotStart>=0&&NewsItems(newsCategoryIndex).Count>0)selected=newsArticleSlotStart+Math.Min(newsArticleIndex,Math.Min(ArticleLimit()-1,NewsItems(newsCategoryIndex).Count-1));
   area=previousArea;UpdateFocus(false);
  }
  WriteState("news_received:"+newsSections.Count);
 }
 void ReadForecast(string json)
 {
  forecast.Clear();
  try{
   using(var data=JsonDocument.Parse(json))foreach(var item in data.RootElement.EnumerateArray()){
    forecast.Add(new ForecastDay{
     Date=item.GetProperty("Date").GetString()??"",
     Code=item.GetProperty("Code").GetInt32(),
     High=item.GetProperty("High").GetString()??"--°",
     Low=item.GetProperty("Low").GetString()??"--°",
     Condition=item.GetProperty("Condition").GetString()??"Temps variable"
    });
    if(forecast.Count==7)break;
   }
  }catch{forecast.Clear();}
 }
 void ReadForecastHours(string json)
 {
  forecastHours.Clear();
  try{
   using(var data=JsonDocument.Parse(json))foreach(var item in data.RootElement.EnumerateArray()){
    forecastHours.Add(new ForecastHour{
     Time=item.GetProperty("Time").GetString()??"--:--",
     Temperature=item.GetProperty("Temperature").GetString()??"--°",
     FeelsLike=item.GetProperty("FeelsLike").GetString()??"--°",
     Precipitation=item.GetProperty("Precipitation").GetString()??"--%",
     Wind=item.GetProperty("Wind").GetString()??"--",
     Day=item.TryGetProperty("Day",out var dayProp)?(dayProp.GetString()??""):"",
     Code=item.GetProperty("Code").GetInt32()
    });
    if(forecastHours.Count==180)break;
   }
  }catch{forecastHours.Clear();}
  forecastHourOffset=Math.Max(0,Math.Min(forecastHourOffset,Math.Max(0,forecastHours.Count-12)));
  forecastHourFocus=Math.Max(0,Math.Min(forecastHourFocus,Math.Min(11,forecastHours.Count-1)));
 }
 void AnimateWeatherIcon(View icon,float baseScale=1)
 {
  if(icon==null)return;
  var frames=new KeyFrames();frames.Add(0,new Vector3(baseScale,baseScale,1));frames.Add(.5f,new Vector3(baseScale*1.07f,baseScale*1.07f,1));frames.Add(1,new Vector3(baseScale,baseScale,1));
  var animation=new Animation(2400){Looping=true};animation.AnimateBetween(icon,"Scale",frames);animation.Play();
  weatherKeyFrames.Add(frames);weatherAnimations.Add(animation);
 }
 void WeatherShape(View icon,List<View> glyphs,int x,int y,int w,int h,Color color,int radius=4)
 {
  var shape=new View{Position2D=new Position2D(x,y),Size2D=new Size2D(w,h),BackgroundColor=color,CornerRadius=new Vector4(radius,radius,radius,radius)};
  icon.Add(shape);glyphs.Add(shape);
 }
 void DrawSun(View icon,List<View> glyphs,Color color)
 {
  var sun=new View{Size2D=new Size2D(78,78)};icon.Add(sun);glyphs.Add(sun);
  var parts=new List<View>();
  WeatherShape(sun,parts,35,13,25,25,color,13);
  WeatherShape(sun,parts,44,1,7,10,color,4);WeatherShape(sun,parts,44,41,7,10,color,4);
  WeatherShape(sun,parts,23,22,10,7,color,4);WeatherShape(sun,parts,62,22,10,7,color,4);
  WeatherShape(sun,parts,28,8,8,7,color,4);WeatherShape(sun,parts,58,8,8,7,color,4);
  WeatherShape(sun,parts,28,37,8,7,color,4);WeatherShape(sun,parts,58,37,8,7,color,4);
  AnimateWeatherScale(sun,1.09f,2100);
 }
 void DrawCloud(View icon,List<View> glyphs,Color color)
 {
  var cloud=new View{Size2D=new Size2D(78,78)};icon.Add(cloud);glyphs.Add(cloud);
  var parts=new List<View>();
  WeatherShape(cloud,parts,17,28,26,23,color,12);WeatherShape(cloud,parts,32,16,30,34,color,15);
  WeatherShape(cloud,parts,50,29,20,20,color,10);WeatherShape(cloud,parts,14,39,58,20,color,10);
  var frames=new KeyFrames();frames.Add(0,new Position(0,0,0));frames.Add(.5f,new Position(3,-2,0));frames.Add(1,new Position(0,0,0));
  AnimateWeatherProperty(cloud,"Position",frames,2800);
 }
 void DrawWeatherIcon()
 {
  if(weatherIcon==null)return;
  ClearWeatherAnimations();
  ClearRainAnimations();
  foreach(var glyph in weatherGlyphs){weatherIcon.Remove(glyph);glyph.Dispose();}weatherGlyphs.Clear();
  DrawWeatherGlyphs(weatherIcon,weatherGlyphs,weatherCode);
 }
 void ClearWeatherAnimations()
 {
  foreach(var animation in weatherAnimations){animation.Stop();animation.Dispose();}weatherAnimations.Clear();
  foreach(var frames in weatherKeyFrames)frames.Dispose();weatherKeyFrames.Clear();
 }
 void ClearRainAnimations()
 {
  foreach(var animation in rainAnimations){animation.Stop();animation.Dispose();}rainAnimations.Clear();
  foreach(var frames in rainKeyFrames)frames.Dispose();rainKeyFrames.Clear();
 }
 View AddWeatherIcon(View parent,int x,int y,int code)
 {
  var icon=new View{Position2D=new Position2D(x,y),Size2D=new Size2D(78,78)};parent.Add(icon);
  DrawWeatherGlyphs(icon,new List<View>(),code);return icon;
 }
 void DrawWeatherGlyphs(View icon,List<View> glyphs,int code)
 {
  var sun=new Color(.98f,.65f,.12f,1);var cloud=new Color(.62f,.69f,.78f,1);
  bool fog=code==45||code==48;
  bool snow=(code>=71&&code<=77)||code==85||code==86;
  bool storm=code>=95;
  bool rain=(code>=51&&code<=67)||(code>=80&&code<=82)||storm;
  if(code==0||code==1||code==2)DrawSun(icon,glyphs,sun);
  if(code!=0)DrawCloud(icon,glyphs,fog||code<0?new Color(.72f,.76f,.81f,1):cloud);
  if(rain)AnimateRainDrops(icon,glyphs,new Color(.25f,.61f,.89f,1));
  if(snow){var flake=new Color(.35f,.68f,.9f,1);WeatherShape(icon,glyphs,24,61,7,7,flake,4);WeatherShape(icon,glyphs,41,64,7,7,flake,4);WeatherShape(icon,glyphs,57,60,7,7,flake,4);}
  if(fog){var haze=new Color(.58f,.65f,.74f,1);WeatherShape(icon,glyphs,18,60,43,4,haze,2);WeatherShape(icon,glyphs,27,68,36,4,haze,2);}
  if(storm)DrawLightning(icon,glyphs,sun);
 }
 void DrawLightning(View icon,List<View> glyphs,Color color)
 {
  var bolt=new View{Position2D=new Position2D(38,44),Size2D=new Size2D(18,24)};icon.Add(bolt);glyphs.Add(bolt);
  bolt.Add(new View{Position2D=new Position2D(7,0),Size2D=new Size2D(9,12),BackgroundColor=color,CornerRadius=new Vector4(3,3,3,3)});
  bolt.Add(new View{Position2D=new Position2D(2,10),Size2D=new Size2D(9,13),BackgroundColor=color,CornerRadius=new Vector4(3,3,3,3)});
  var frames=new KeyFrames();frames.Add(0,0f);frames.Add(.08f,1f);frames.Add(.18f,.12f);frames.Add(.28f,1f);frames.Add(.42f,.12f);frames.Add(1,0f);
  AnimateWeatherProperty(bolt,"Opacity",frames,1700);
 }
 void AnimateWeatherScale(View target,float peak,int duration)
 {
  var frames=new KeyFrames();frames.Add(0,new Vector3(.96f,.96f,1));frames.Add(.5f,new Vector3(peak,peak,1));frames.Add(1,new Vector3(.96f,.96f,1));
  AnimateWeatherProperty(target,"Scale",frames,duration);
 }
 void AnimateWeatherProperty(View target,string property,KeyFrames frames,int duration)
 {
  var animation=new Animation(duration){Looping=true};animation.AnimateBetween(target,property,frames);animation.Play();
  weatherKeyFrames.Add(frames);weatherAnimations.Add(animation);
 }
 void AnimateRainDrops(View icon,List<View> glyphs,Color color)
 {
  int x=24,y=52;
  var drops=new View{Position2D=new Position2D(x,y),Size2D=new Size2D(36,27)};icon.Add(drops);glyphs.Add(drops);
  foreach(var point in new[]{new Position2D(0,0),new Position2D(15,3),new Position2D(30,0)})
   drops.Add(new View{Position2D=point,Size2D=new Size2D(5,12),BackgroundColor=color,CornerRadius=new Vector4(3,3,3,3)});
  var position=new KeyFrames();position.Add(0,new Position(x,y,0));position.Add(1,new Position(x,y+15,0));
  var opacity=new KeyFrames();opacity.Add(0,1f);opacity.Add(.72f,.58f);opacity.Add(1,.08f);
  var animation=new Animation(1100){Looping=true};animation.AnimateBetween(drops,"Position",position);animation.AnimateBetween(drops,"Opacity",opacity);animation.Play();
  rainKeyFrames.Add(position);rainKeyFrames.Add(opacity);rainAnimations.Add(animation);
 }
 string WeatherDescription(int code)
 {
  if(code==0)return "Ciel dégagé";
  if(code==1)return "Peu nuageux";
  if(code==2)return "Partiellement nuageux";
  if(code==3)return "Couvert";
  if(code==45||code==48)return "Brouillard";
  if(code==51||code==56)return "Bruine légère";
  if(code==53||code==57)return "Bruine";
  if(code==55)return "Bruine dense";
  if(code==61||code==66)return "Pluie légère";
  if(code==63)return "Pluie modérée";
  if(code==65||code==67)return "Pluie forte";
  if(code==71||code==77)return "Neige légère";
  if(code==73)return "Neige modérée";
  if(code==75)return "Neige forte";
  if(code==80)return "Averses légères";
  if(code==81)return "Averses";
  if(code==82)return "Averses fortes";
  if(code==85)return "Averses de neige";
  if(code==86)return "Fortes averses de neige";
  if(code==95||code==96||code==99)return "Orage";
  return "Temps variable";
 }
 void BuildPage()
 {
  var stopwatch=Stopwatch.StartNew();
  if(focusAnimation!=null){focusAnimation.Stop();focusAnimation.Dispose();focusAnimation=null;}
  ClearWeatherAnimations();ClearRainAnimations();hourCards.Clear();dayCards.Clear();
  ParkArtwork();pageArtworkCold=0;
  foreach(var v in scene){Window.Default.Remove(v);v.Dispose();}scene.Clear();slots.Clear();restBg.Clear();backlightHint=null;ihTop=null;homeSensorValue=null;weatherRain=null;metroFoot=null;weatherGlyphs.Clear();weatherCard=null;weatherIcon=null;weatherSlotIndex=-1;metroCard=null;metroSlotIndex=-1;marketSlotIndex=-1;factSlotIndex=-1;newsButtonSlotIndex=-1;newsCategorySlotStart=-1;newsArticleSlotStart=-1;newsTopTitle=null;marketHeadline=null;marketValue=null;marketChange=null;marketTime=null;marketSub=null;marketChartHost=null;cinemaState=null;felixState=null;cinemaCard=null;cinemaDot=null;factBody=null;metroStatus=null;metroNationTime=null;metroEtoileTime=null;weatherPlace=null;weatherTemperature=null;weatherCondition=null;weatherRange=null;heroTop=null;heroTitle=null;heroBackdrop=null;heroBackdropImage=null;heroButton=null;heroSub=null;heroTime=null;heroArt=null;heroBar=null;heroFill=null;heroArtPath="";worldParis=null;worldBa=null;
  if(page!=0)homeShelf=0;
  AddWallpaper();
  if(page==0&&homeShelf==0)BuildHeroBackdrop();
  // Header (BelAir OS 2): brand mark, one floating glass tab bar in the middle, round glass buttons on the right.
  var brandMark=Panel(96,52,52,52,accent,16);var brandLetter=Label("B",0,0,52,52,28,white,"Semibold");brandLetter.HorizontalAlignment=HorizontalAlignment.Center;brandMark.Add(brandLetter);
  int pillW=12;for(int i=0;i<tabNav.Length;i++)pillW+=tabPillW[i]+(i>0?4:0);pillW+=12;int pillX=(1920-pillW)/2;
  var pill=Panel(pillX,42,pillW,68,GlassFill(),34);pill.BorderlineWidth=1;pill.BorderlineColor=GlassEdge();LiquidGlass(pill);
  tabs=new TextLabel[tabNav.Length];
  for(int i=0,tx=pillX+12;i<tabNav.Length;tx+=tabPillW[i]+4,i++){tabs[i]=Text(tabNav[i],tx,48,tabPillW[i],56,22,muted);tabs[i].HorizontalAlignment=HorizontalAlignment.Center;}
  if(page!=0){clock=Text(DateTime.Now.ToString("HH:mm"),1540,132,300,44,28,ink,"Semibold");clock.HorizontalAlignment=HorizontalAlignment.End;}
  if(page!=0&&page!=5)Text(PageTitle(),96,124,1200,72,56,ink,"Display");
  if(page==0){
   // BelAir OS 2 Accueil: what is playing, big, straight on the backdrop; then the "Regarder" and "Infos" rows.
   clock=null;heroArtPath=null;
   if(homeShelf==0){
    // Main mockup: what is playing, big on the full-bleed backdrop, two actions, then the "Regarder" shelf
    heroTop=Text("ENTRÉE ACTIVE",96,312,1100,34,22,muted,"Semibold");
    heroTitle=Text("Apple TV",90,344,1300,140,104,ink,"Display");heroTitle.Ellipsis=false;
    heroSub=Text("",96,486,1100,40,28,muted,"Medium");
    heroBar=(View)Add(new View{Position2D=new Position2D(96,548),Size2D=new Size2D(420,6),BackgroundColor=new Color(1,1,1,.25f),CornerRadius=new Vector4(3,3,3,3),Opacity=0});
    heroFill=new View{Size2D=new Size2D(0,6),BackgroundColor=white,CornerRadius=new Vector4(3,3,3,3)};heroBar.Add(heroFill);
    heroTime=Text("",536,534,400,34,22,muted,"Medium");
    var hb=Panel(96+200,590+36,400,72,new Color(1,1,1,.80f),36);hb.PivotPoint=new Position(.5f,.5f,.5f);hb.PositionUsesPivotPoint=true;hb.BorderlineWidth=1.5f;hb.BorderlineColor=new Color(1,1,1,.95f);LiquidGlass(hb,26f);
    hb.Add(new ImageView{ResourceUrl=IconFile("play","#0B0F19"),Position2D=new Position2D(40,25),Size2D=new Size2D(22,22)});
    heroButton=Label("Passer sur l’Apple TV",72,0,300,72,24,new Color(.04f,.06f,.10f,1),"Semibold");hb.Add(heroButton);
    slots.Add(new Slot{Card=hb,Name="Apple TV",Action="hdmi:13",Description="Entrée HDMI 1 · Apple TV",Row=0,X=96,Y=590,W=400,H=72});
    // TV backlight control: shows its state, OK opens its light sheet
    int bw2=400;var fb=Panel(520+bw2/2,590+36,bw2,72,dark?new Color(1,1,1,.16f):new Color(1,1,1,.50f),36);fb.PivotPoint=new Position(.5f,.5f,.5f);fb.PositionUsesPivotPoint=true;fb.BorderlineWidth=1.5f;fb.BorderlineColor=new Color(1,1,1,dark?.32f:.9f);LiquidGlass(fb,26f);
    bool blOn=IsLampOn(backlightId);
    var bdot=new View{Position2D=new Position2D(18,14),Size2D=new Size2D(44,44),CornerRadius=new Vector4(22,22,22,22),BackgroundColor=blOn?HexColor(lightRgb.TryGetValue(backlightId,out var brh)&&brh!=""?brh:"#FFB547"):dark?new Color(1,1,1,.14f):new Color(0,0,0,.08f)};fb.Add(bdot);
    bdot.Add(new ImageView{ResourceUrl=IconFile("tv",blOn?"#16130F":HexOf(muted)),Position2D=new Position2D(10,10),Size2D=new Size2D(24,24)});
    fb.Add(Label("Rétroéclairage",76,8,bw2-96,32,23,ink,"Semibold"));
    backlightHint=Label(LampStateText(backlightId),76,38,bw2-96,26,18,muted,"Medium");fb.Add(backlightHint);
    slots.Add(new Slot{Card=fb,Name="Rétroéclairage TV",Action="light:"+backlightId,Description="OK pour la luminosité, les scènes et les options du rétroéclairage",Row=0,X=520,Y=590,W=bw2,H=72});
    UpdateHero();
    Text("Regarder",96,786,600,40,26,ink,"Semibold");
   }else{
    // Infos mockup: the selected widget fills the top of the screen, the Infos shelf, then the Maison scenes shelf
    BuildInfoHero();
    Text("Infos",96,540,600,40,26,ink,"Semibold");
    BuildWeatherWidget();
    BuildMetroWidget();
    BuildMarketTile();
    BuildFactTile();
    BuildHomeSensors();
    Text("Maison",96,808,600,40,26,ink,"Semibold");
    BuildHomeShelf();
   }
  }else if(page==1){
   BuildLibrary();
  }else if(page==2){
   RefreshSettings();
   SettingsTile("Volume","volume","OK · flèches gauche/droite","volume",0,0);
   SettingsTile("Luminosité","brightness","OK · flèches, paliers de 5","brightness",1,0);
   SettingsTile("Contraste","contrast","OK · flèches, paliers de 5","contrast",2,0);
   SettingsTile("Mode d’image","picturemode","OK pour choisir le mode","picture",3,0);
   SettingsTile("Mode Jeu","game-mode","OK pour activer ou désactiver","game",0,1);
   SettingsTile("Son","mute","OK pour couper ou rétablir le son","mute",1,1);
   SettingsTile("Minuterie","sleeptimer","OK pour modifier la durée","sleep",2,1);
   SettingsTile("Réseau","network","Wi-Fi et connexion","network",3,1);
   AppearanceTile("Paramètres TV","app:org.tizen.menu","Ouvrir les réglages Samsung",0);
   AppearanceTile("Manettes","controllers","DualSense · maintenir PS + Create",1);
   AppearanceTile("Thème","pref:theme","Automatique : clair du lever au coucher du soleil",2);
   AppearanceTile("Couleur d’accent","pref:accent","Focus, boutons et graphiques",3);
   AppearanceTile("Fond d’écran","pref:wall","OK pour choisir parmi les fonds d’écran",4);
   AppearanceTile("Brief du matin","pref:brief","Résumé au premier allumage, de 5 h à 11 h",5);
   if(pictureOpen)BuildPicturePopup();
   if(appearOpen)BuildAppearance();
  }else if(page==3)BuildForecast();else if(page==4)BuildMetroDetail();else if(page==6)BuildSystemInfo();else if(page==7)BuildEnergy();else if(page==8)BuildPlex();else if(page==9)BuildFelix();else if(page==10)BuildBikes();else if(page==11)BuildFind();else BuildHomeAssistant();
  if(page==0){
  var dk=new[]{new[]{"Netflix","atv:org.tizen.netflix-app:Netflix","netflix"},new[]{"YouTube","atv:com.samsung.tv.cobalt-yt:YouTube","youtube"},new[]{"CANAL+","atv:QmpwflxWdt.CanalPlusLauncher:CANAL+","canal-plus"},new[]{"Plex","atv:kIciSQlYEM.plex:Plex","plex"},new[]{"Prime Video","atv:org.tizen.primevideo:Prime Video","prime"},new[]{"Apple TV","atv:com.samsung.tv.aria-video:TV","apple-tv"},new[]{"France 2 en direct","channel:france-2","france-2"}};
  var dord=new List<int>();for(int i=0;i<dk.Length;i++)dord.Add(i);dord.Sort((x,y)=>{int ux=Use(UsageKey(dk[x][1])),uy=Use(UsageKey(dk[y][1]));return ux!=uy?uy-ux:x-y;});
  if(homeShelf==0)for(int i=0;i<dord.Count;i++)Dock(dk[dord[i]][0],dk[dord[i]][1],dk[dord[i]][2],i,dk.Length);
  }
  if(!((page==5||page==0)&&lightOpen)&&!(page==5&&coverPopupOpen)&&!(page==2&&appearOpen)){Toolbar("Applis","page:1","apps",0);Toolbar("Accueil","page:0","hdmi",1);Toolbar("Réglages","page:2","settings",2);}
  if(page==0){NewsDropdownButton();WorldClock();if(lightOpen){BuildLightPopup();if(catalogOpen)BuildSceneCatalog();}}
  toolbarHint=Text("",1530,170,310,45,24,ink);toolbarHint.HorizontalAlignment=HorizontalAlignment.End;
  detail=Text("",80,1030,1320,35,20,muted);
  notice=Text("",80,1008,1760,34,22,accent);
  string hintText=newsMenuOpen?(newsDetailOpen?"Retour pour revenir aux actualités":"OK pour ouvrir · Retour pour fermer"):page==1?(AppLibrary.PageCount(library.Count)<=1?"OK pour ouvrir · Retour à l’accueil":libraryPage+1<AppLibrary.PageCount(library.Count)?"Bas pour voir la suite · Retour à l’accueil":"Haut pour la page précédente · Retour à l’accueil"):page==3?"↓ heures · ←/→ défiler · Retour fermer":page==4||page==8||page==9||page==10||page==11?"Retour à l’accueil":"Retour à l’entrée";
  var hint=Text(hintText,1310,1030,530,35,20,muted);hint.HorizontalAlignment=HorizontalAlignment.End;
  if(page==0&&newsMenuOpen){if(newsDetailOpen)BuildNewsDetail();else{BuildNewsDropdown();if(quoteDetailOpen)BuildQuoteDetail();}}
  if(page==0&&factDetailOpen)BuildFactDetail();
  selected=Math.Max(0,Math.Min(selected,slots.Count-1));UpdateFocus(false);
  if(page==10&&mapSel>=0)ApplyMapMode();
  if(page!=11&&findUnlocked){findUnlocked=false;pinEntry="";findFocus="";findZoom=-1;panWx=0;panWy=0;findPan=false;}
  buildMilliseconds=stopwatch.ElapsedMilliseconds;WriteState("page_ready");
 }
 void BuildForecast()
 {
  Text("Prévision du jour",80,246,760,42,30,ink,"Semibold");
  var current=Panel(80,294,1760,126,GlassFill(),24);
  var currentIcon=AddWeatherIcon(current,20,24,weatherCode);AnimateWeatherIcon(currentIcon);
  var nowTemp=Label(weatherTemperatureText,114,8,160,78,52,ink,"Semibold");nowTemp.Ellipsis=false;current.Add(nowTemp);
  current.Add(Label("Maintenant · "+weatherConditionText,265,25,680,42,27,ink,"Semibold"));
  var range=Label("Aujourd’hui · "+WeatherRange(),1040,19,680,38,22,muted,"Semibold");
  range.HorizontalAlignment=HorizontalAlignment.End;range.Ellipsis=false;current.Add(range);
  string nowDetails=forecastHours.Count==0?"Détails horaires indisponibles":"Ressenti "+forecastHours[0].FeelsLike+" · Pluie "+forecastHours[0].Precipitation+" · Vent "+forecastHours[0].Wind+" km/h";
  current.Add(Label(nowDetails,265,76,1440,32,20,muted,"Medium"));
  Text("Par heure · température, ressenti, pluie et vent",80,432,1100,34,26,ink,"Semibold");
  if(forecastHours.Count==0){Text("Prévisions horaires indisponibles.",80,477,900,45,24,muted);}
  else{
   int visible=Math.Min(12,forecastHours.Count-forecastHourOffset),hourCardWidth=134,hourGap=13,hourCardY=474,hourCardHeight=214;
   for(int i=0;i<visible;i++){
    var hour=forecastHours[forecastHourOffset+i];int x=80+i*(hourCardWidth+hourGap);
    var card=Panel(x,hourCardY,hourCardWidth,hourCardHeight,GlassFill(),18);
    card.BorderlineWidth=area==5&&i==forecastHourFocus?4:0;card.BorderlineColor=accent;hourCards.Add(card);
    var time=Label(hour.Time,6,7,hourCardWidth-12,25,18,ink,"Semibold");time.HorizontalAlignment=HorizontalAlignment.Center;card.Add(time);
    AddWeatherIcon(card,28,31,hour.Code);
    var temperature=Label(hour.Temperature,6,107,hourCardWidth-12,34,23,ink,"Semibold");temperature.HorizontalAlignment=HorizontalAlignment.Center;temperature.Ellipsis=false;card.Add(temperature);
    var feels=Label("Ress. "+hour.FeelsLike,2,140,hourCardWidth-4,24,17,muted);feels.HorizontalAlignment=HorizontalAlignment.Center;feels.Ellipsis=false;card.Add(feels);
    var rain=Label("Pluie "+hour.Precipitation,2,163,hourCardWidth-4,24,17,muted);rain.HorizontalAlignment=HorizontalAlignment.Center;rain.Ellipsis=false;card.Add(rain);
    var wind=Label("Vent "+hour.Wind+" km/h",2,186,hourCardWidth-4,24,17,muted);wind.HorizontalAlignment=HorizontalAlignment.Center;wind.Ellipsis=false;card.Add(wind);
   }
  }
  Text("Prévisions sur 7 jours · aujourd’hui compris",80,707,900,36,27,ink,"Semibold");
  if(forecast.Count==0)Text("Prévisions indisponibles. Vérifiez la connexion Internet.",80,750,1200,45,24,muted);
  else{
   var culture=frCulture;
   int cardY=751,cardW=236,cardH=258,gap=18;
   for(int i=0;i<Math.Min(7,forecast.Count);i++){
    var day=forecast[i];int x=80+i*(cardW+gap);
   var card=Panel(x,cardY,cardW,cardH,GlassFill(),22);dayCards.Add(card);
    DateTime date;bool parsed=DateTime.TryParseExact(day.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date);
    string dayName=i==0?"Aujourd’hui":parsed?date.ToString("ddd",culture):"Jour "+(i+1);
    string dateText=parsed?date.ToString("d MMM",culture):day.Date;
    var name=Label(dayName,12,8,cardW-24,32,21,ink,"Semibold");name.HorizontalAlignment=HorizontalAlignment.Center;card.Add(name);
    var dateLabel=Label(dateText,12,37,cardW-24,27,18,muted);dateLabel.HorizontalAlignment=HorizontalAlignment.Center;card.Add(dateLabel);
    var icon=AddWeatherIcon(card,79,64,day.Code);if(i==0)AnimateWeatherIcon(icon);
    var condition=Label(day.Condition,12,143,cardW-24,36,17,ink,"Semibold");condition.HorizontalAlignment=HorizontalAlignment.Center;card.Add(condition);
    var high=Label("Max  "+day.High,28,187,180,31,22,ink,"Semibold");card.Add(high);
    card.Add(Label("Min  "+day.Low,28,220,180,30,21,muted,"Medium"));
   }
  }
  if(forecastDetailOpen)BuildWeatherDetail();
 }
 void BuildMetroDetail()
 {
  if(DateTime.Now-lastLinesRequest>=TimeSpan.FromSeconds(45)){lastLinesRequest=DateTime.Now;RequestLines();}
  var red=new Color(.89f,.22f,.17f,1);var orange=new Color(.96f,.57f,.11f,1);var green=new Color(.22f,.66f,.22f,1);
  // live card for line 6 at Bel-Air
  var live=Panel(80,240,1060,286,new Color(.10f,.16f,.27f,1),26);
  live.Add(new View{Position2D=new Position2D(30,28),Size2D=new Size2D(68,68),CornerRadius=new Vector4(34,34,34,34),BackgroundColor=HexColor("#83C491")});
  var six=Label("6",30,28,68,68,38,TileTitleColor("#83C491"),"Semibold");six.HorizontalAlignment=HorizontalAlignment.Center;live.Add(six);
  live.Add(Label("Bel-Air",116,24,500,46,36,white,"Semibold"));
  live.Add(Label("Prochains passages · temps réel",118,68,600,30,19,new Color(.78f,.85f,.94f,1),"Medium"));
  int st6=lineStatus.TryGetValue("metro/6",out var s6)?s6:(trafficHasAlert?1:0);
  var pill=new View{Position2D=new Position2D(1060-30-230,36),Size2D=new Size2D(230,48),CornerRadius=new Vector4(24,24,24,24),BackgroundColor=st6>=2?red:st6==1?orange:new Color(.16f,.55f,.33f,1)};live.Add(pill);
  var pl=Label(st6>=2?"TRÈS PERTURBÉ":st6==1?"PERTURBÉ":"TRAFIC NORMAL",0,0,230,48,18,white,"Semibold");pl.HorizontalAlignment=HorizontalAlignment.Center;pill.Add(pl);
  string[] dirs={"Nation","Etoile"},dirNames={"Nation","Charles-de-Gaulle–Étoile"};
  for(int d=0;d<2;d++){
   int x=30+d*515;var col=new View{Position2D=new Position2D(x,118),Size2D=new Size2D(485,140),BackgroundColor=new Color(1,1,1,.08f),CornerRadius=new Vector4(18,18,18,18)};live.Add(col);
   col.Add(Label("→ "+dirNames[d],20,10,445,32,21,new Color(.86f,.91f,.97f,1),"Semibold"));
   var next=new List<MetroDeparture>();foreach(var dep in metroDepartures)if(dep.Destination.IndexOf(dirs[d],StringComparison.OrdinalIgnoreCase)>=0&&next.Count<2)next.Add(dep);
   if(next.Count==0){col.Add(Label(metroAvailable?"Pas de passage annoncé":"Horaires indisponibles",20,52,445,60,24,new Color(.78f,.85f,.94f,1),"Medium"));continue;}
   string big=next[0].AtStop?"À quai":next[0].Minutes==0?"< 1 min":next[0].Minutes+" min";
   col.Add(Label(big,18,42,260,64,50,white,"Display"));
   if(next[0].Time!=""){var tl=Label(next[0].Time,300,58,165,40,24,new Color(.86f,.91f,.97f,1),"Semibold");tl.HorizontalAlignment=HorizontalAlignment.End;col.Add(tl);}
   if(next.Count>1)col.Add(Label("puis "+(next[1].AtStop?"à quai":next[1].Minutes==0?"< 1 min":next[1].Minutes+" min")+(next[1].Time!=""?" · "+next[1].Time:""),20,102,445,30,19,new Color(.78f,.85f,.94f,1),"Medium"));
  }
  // network summary
  int ok=0,warn=0,bad=0;var troubled=new List<string>();
  for(int i=0;i<rerCodes.Length;i++){int v=lineStatus.TryGetValue("rer/"+rerCodes[i],out var x)?x:0;if(v==0)ok++;else{if(v==1)warn++;else bad++;troubled.Add("RER "+rerCodes[i]);}}
  for(int i=0;i<metroCodes.Length;i++){int v=lineStatus.TryGetValue("metro/"+metroCodes[i],out var x)?x:0;if(v==0)ok++;else{if(v==1)warn++;else bad++;troubled.Add("M"+metroCodes[i]);}}
  var sum=Panel(1166,240,674,286,GlassFill(),26);
  string timeStr=(linesUpdated==DateTime.MinValue?DateTime.Now:linesUpdated).ToString("HH:mm",CultureInfo.InvariantCulture);
  sum.Add(Label("RÉSEAU MÉTRO ET RER",30,24,420,26,17,muted,"Semibold"));
  var upd=Label("à "+timeStr,420,24,224,26,17,muted,"Medium");upd.HorizontalAlignment=HorizontalAlignment.End;sum.Add(upd);
  sum.Add(Label(warn+bad==0?"Tout roule":ok+" lignes sur 21 normales",28,56,620,64,warn+bad==0?50:40,ink,"Display"));
  int[] counts={ok,warn,bad};string[] names={"normales","perturbées","très perturbées"};Color[] cols={green,orange,red};
  for(int i=0;i<3;i++){int y=136+i*40;sum.Add(new View{Position2D=new Position2D(32,y+8),Size2D=new Size2D(20,20),CornerRadius=new Vector4(10,10,10,10),BackgroundColor=cols[i]});sum.Add(Label(counts[i]+" "+(counts[i]<=1?names[i].TrimEnd('s'):names[i]),64,y,560,36,22,counts[i]>0||i==0?ink:muted,"Medium"));}
  // all lines
  string[] codes=new string[rerCodes.Length+metroCodes.Length];string[] kinds=new string[codes.Length];string[] hexes=new string[codes.Length];
  for(int i=0;i<rerCodes.Length;i++){codes[i]=rerCodes[i];kinds[i]="rer";hexes[i]=rerHex[i];}
  for(int i=0;i<metroCodes.Length;i++){codes[rerCodes.Length+i]=metroCodes[i];kinds[rerCodes.Length+i]="metro";hexes[rerCodes.Length+i]=metroHex[i];}
  int tw=234,th=132,gx=20,gy=16;
  for(int i=0;i<codes.Length;i++){int col=i%7,row=i/7;LineTile(kinds[i],codes[i],hexes[i],80+col*(tw+gx),552+row*(th+gy),tw,th,row);}
  if(lineDetailOpen)BuildLineDetail();
 }
 void LineTile(string kind,string label,string hex,int x,int y,int w,int h,int row)
 {
  string key=kind+"/"+label;int st=lineStatus.TryGetValue(key,out var s)?s:0;
  var fc=st>=2?new Color(.89f,.22f,.17f,1):st==1?new Color(.96f,.57f,.11f,1):new Color(.22f,.66f,.22f,1);
  var card=Panel(x+w/2,y+h/2,w,h,st==0?GlassFill():new Color(fc.R,fc.G,fc.B,dark?.22f:.16f),20);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
  card.Add(new View{Position2D=new Position2D(0,18),Size2D=new Size2D(6,h-36),BackgroundColor=fc,CornerRadius=new Vector4(3,3,3,3)});
  int bs=56,br=kind=="metro"?bs/2:12;
  card.Add(new View{Position2D=new Position2D(20,16),Size2D=new Size2D(bs,bs),CornerRadius=new Vector4(br,br,br,br),BackgroundColor=HexColor(hex)});
  var t=Label(label,20,16,bs,bs,label.Length>=4?16:label.Length>=3?20:28,TileTitleColor(hex),"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;card.Add(t);
  card.Add(Label(kind=="metro"?"MÉTRO":"RER",90,16,130,22,14,muted,"Semibold"));
  card.Add(Label(st>=2?"Très perturbé":st==1?"Perturbé":"Normal",90,38,138,32,st==0?21:20,st==0?ink:fc,"Semibold"));
  string hdr=lineHeader.TryGetValue(key,out var hv)?hv:"";
  var msg=Label(st==0?"Aucune perturbation":Clip(hdr==""?"Voir le détail":hdr,52),20,82,w-36,44,15,st==0?muted:ink,"Medium");msg.MultiLine=true;msg.LineWrapMode=LineWrapMode.Word;msg.VerticalAlignment=VerticalAlignment.Top;card.Add(msg);
  slots.Add(new Slot{Card=card,Name=(kind=="metro"?"Métro ":"RER ")+label,Action="line:"+key,Description=st==0?"Trafic normal":"OK pour le détail",Row=row,X=x,Y=y,W=w,H=h});
 }
 void AddLegendDot(View parent,int x,Color color,string glyph)
 {
  var dot=new View{Position2D=new Position2D(x,15),Size2D=new Size2D(24,24),CornerRadius=new Vector4(12,12,12,12),BackgroundColor=color};parent.Add(dot);
  if(glyph!=""){var g=Label(glyph,x,13,24,24,15,white,"Semibold");g.HorizontalAlignment=HorizontalAlignment.Center;parent.Add(g);}
 }
 void BuildLineBadge(string kind,string label,string hex,int cx,int cy,int size,int rowN)
 {
  string key=kind+"/"+label;int st=lineStatus.TryGetValue(key,out var s)?s:0;
  var fc=st>=2?new Color(.89f,.22f,.17f,1):st==1?new Color(.96f,.57f,.11f,1):new Color(.22f,.66f,.22f,1);
  int cs=size+14,fo=(cs-size)/2;
  var card=Panel(cx,cy,cs,cs,new Color(0,0,0,0),20);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
  card.Add(new View{Position2D=new Position2D(fo,fo),Size2D=new Size2D(size,size),CornerRadius=new Vector4(18,18,18,18),BackgroundColor=white,BorderlineWidth=5,BorderlineColor=fc});
  int inset=11,bsz=size-inset*2,br=kind=="metro"?bsz/2:13;
  card.Add(new View{Position2D=new Position2D(fo+inset,fo+inset),Size2D=new Size2D(bsz,bsz),CornerRadius=new Vector4(br,br,br,br),BackgroundColor=HexColor(hex)});
  var t=Label(label,fo,fo,size,size,label.Length>=4?18:label.Length>=3?22:34,TileTitleColor(hex),"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;card.Add(t);
  if(st>=1){int bd=30,bx=cs-bd-1,by=cs-bd-1;
   card.Add(new View{Position2D=new Position2D(bx,by),Size2D=new Size2D(bd,bd),CornerRadius=new Vector4(bd/2,bd/2,bd/2,bd/2),BackgroundColor=fc,BorderlineWidth=3,BorderlineColor=white});
   var bt=Label(st>=2?"×":"!",bx,by-2,bd,bd,st>=2?24:22,white,"Semibold");bt.HorizontalAlignment=HorizontalAlignment.Center;card.Add(bt);}
  slots.Add(new Slot{Card=card,Name=(kind=="metro"?"Métro ":"RER ")+label,Action="line:"+key,Description="OK pour le détail",Row=rowN,X=cx-cs/2,Y=cy-cs/2,W=cs,H=cs});
 }
 void BuildLineDetail()
 {
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.05f,.07f,.12f,.6f)});
  int pw=1320,ph=560,px=(1920-pw)/2,py=(1080-ph)/2;
  var panel=Panel(px+pw/2,py+ph/2,pw,ph,dark?new Color(.10f,.11f,.15f,.97f):new Color(.99f,.98f,.96f,.98f),44);panel.PivotPoint=new Position(.5f,.5f,.5f);panel.PositionUsesPivotPoint=true;panel.BorderlineWidth=1;panel.BorderlineColor=GlassEdge();panel.BoxShadow=new Shadow(80f,new Color(0,0,0,.35f),new Vector2(0,30));
  var parts=lineDetailKey.Split('/');string kind=parts[0],label=parts.Length>1?parts[1]:"";
  string hex="#555";if(kind=="metro"){int idx=Array.IndexOf(metroCodes,label);if(idx>=0)hex=metroHex[idx];}else{int idx=Array.IndexOf(rerCodes,label);if(idx>=0)hex=rerHex[idx];}
  int pbr=kind=="metro"?39:15;
  panel.Add(new View{Position2D=new Position2D(48,44),Size2D=new Size2D(78,78),CornerRadius=new Vector4(pbr,pbr,pbr,pbr),BackgroundColor=HexColor(hex)});
  var bl=Label(label,48,44,78,78,label.Length>=4?20:label.Length>=3?24:36,TileTitleColor(hex),"Semibold");bl.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(bl);
  int st=lineStatus.TryGetValue(lineDetailKey,out var s)?s:0;
  panel.Add(Label((kind=="metro"?"Métro ":"RER ")+label,144,46,900,42,32,ink,"Semibold"));
  var stc=st>=2?new Color(.85f,.2f,.15f,1):st==1?new Color(.82f,.48f,.05f,1):new Color(.13f,.58f,.3f,1);
  panel.Add(Label(st>=2?"Fortement perturbé":st==1?"Trafic perturbé":"Trafic normal",144,94,900,36,24,stc,"Semibold"));
  string hdr=lineHeader.TryGetValue(lineDetailKey,out var h)?h:"",dsc=lineDesc.TryGetValue(lineDetailKey,out var d)?d:"";
  if(st==0)panel.Add(Label("Aucune perturbation signalée sur la ligne.",48,170,pw-96,40,24,muted,"Medium"));
  else{
   var hl=Label(hdr,48,160,pw-96,96,25,ink,"Semibold");hl.MultiLine=true;hl.LineWrapMode=LineWrapMode.Word;hl.VerticalAlignment=VerticalAlignment.Top;panel.Add(hl);
   var dl=Label(dsc,48,266,pw-96,ph-326,21,Tx(.3f,.32f,.37f,1),"Medium");dl.MultiLine=true;dl.LineWrapMode=LineWrapMode.Word;dl.VerticalAlignment=VerticalAlignment.Top;panel.Add(dl);
  }
  var hint=Label("Retour pour fermer",0,ph-48,pw,28,20,muted,"Medium");hint.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(hint);
 }
 void ReceiveLines(string json)
 {
  try{using(var doc=JsonDocument.Parse(json))foreach(var el in doc.RootElement.EnumerateArray()){
   string kind=el.GetProperty("kind").GetString(),label=el.GetProperty("label").GetString();if(kind==null||label==null)continue;
   string key=kind+"/"+label;lineStatus[key]=el.GetProperty("status").GetInt32();
   lineHeader[key]=el.TryGetProperty("header",out var h)?(h.GetString()??""):"";
   lineDesc[key]=el.TryGetProperty("desc",out var d)?(d.GetString()??""):"";
  }}catch{}
  linesUpdated=DateTime.Now;
  if(page==4&&!lineDetailOpen)BuildPage();
  WriteState("lines_data");
 }
 void RequestLines()
 {
  lastLinesRequest=DateTime.Now;SendDataRequest(false,false,false,false,true);
 }
 void CollectSystemInfo()
 {
  try{long total=0,avail=0,free=0,buffers=0,cached=0;
   foreach(var line in File.ReadAllLines("/proc/meminfo")){int c=line.IndexOf(':');if(c<0)continue;string k=line.Substring(0,c),v=line.Substring(c+1).Trim();int sp=v.IndexOf(' ');long kb;if(!long.TryParse(sp>0?v.Substring(0,sp):v,out kb))continue;
    if(k=="MemTotal")total=kb;else if(k=="MemAvailable")avail=kb;else if(k=="MemFree")free=kb;else if(k=="Buffers")buffers=kb;else if(k=="Cached")cached=kb;}
   if(avail==0)avail=free+buffers+cached;memTotalKb=total;memUsedKb=total-avail;memPct=total>0?100.0*memUsedKb/total:0;}catch{}
  try{var t=File.ReadAllLines("/proc/stat")[0].Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
   ulong idle=ulong.Parse(t[4])+(t.Length>5?ulong.Parse(t[5]):0),tot=0;for(int i=1;i<t.Length;i++)tot+=ulong.Parse(t[i]);
   if(lastCpuTotal>0&&tot>lastCpuTotal){ulong dt=tot-lastCpuTotal,di=idle>=lastCpuIdle?idle-lastCpuIdle:0;sysCpuPct=Math.Round(100.0*(dt-di)/dt).ToString(CultureInfo.InvariantCulture)+" %";}
   lastCpuTotal=tot;lastCpuIdle=idle;}catch{}
  try{var la=File.ReadAllText("/proc/loadavg").Split(' ');sysLoad=la[0]+"   "+la[1]+"   "+la[2];}catch{}
  try{double secs=double.Parse(File.ReadAllText("/proc/uptime").Split(' ')[0],CultureInfo.InvariantCulture);var ts=TimeSpan.FromSeconds(secs);sysUptime=(ts.Days>0?ts.Days+" j ":"")+ts.Hours+" h "+ts.Minutes+" min";}catch{}
  if(sysCpuCores==0)try{int cores=0;string model="";foreach(var line in File.ReadAllLines("/proc/cpuinfo")){if(line.StartsWith("processor"))cores++;else if(model==""&&(line.StartsWith("model name")||line.StartsWith("Hardware")||line.StartsWith("Processor"))){int c=line.IndexOf(':');if(c>=0)model=line.Substring(c+1).Trim();}}sysCpuCores=cores>0?cores:1;sysCpuModel=(model==""?"Processeur":model)+" · "+sysCpuCores+" cœurs";}catch{}
  if(sysHost=="—")try{sysHost=File.ReadAllText("/proc/sys/kernel/hostname").Trim();}catch{}
  if(sysKernel=="—")try{var p=File.ReadAllText("/proc/version").Split(' ');sysKernel=p.Length>=3?p[2]:p[0];}catch{}
  if(sysOs=="—"||sysBuild=="—")try{string ver="",cpe="";
   foreach(var line in File.ReadAllLines("/etc/os-release")){int c=line.IndexOf('=');if(c<0)continue;string k=line.Substring(0,c),v=line.Substring(c+1).Trim().Trim('"');
    if(k=="VERSION_ID")ver=v;else if(k=="CPE_NAME")cpe=v;else if(k=="BUILD_ID")sysBuild=v;}
   if(ver==""&&cpe!=""){int c=cpe.LastIndexOf(':');if(c>=0)ver=cpe.Substring(c+1);}
   if(ver!="")sysOs="Tizen "+ver;}catch{}
  if(sysIp=="—"||(DateTime.Now-lastNetScan).TotalSeconds>=30){lastNetScan=DateTime.Now;CollectNetwork();}
  if(sysParts.Count==0)CollectPartitions();
 }
 void CollectNetwork()
 {
  try{foreach(var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()){
    if(ni.OperationalStatus!=System.Net.NetworkInformation.OperationalStatus.Up||ni.NetworkInterfaceType==System.Net.NetworkInformation.NetworkInterfaceType.Loopback)continue;
    foreach(var ua in ni.GetIPProperties().UnicastAddresses){if(ua.Address.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork)continue;
     sysIp=ua.Address.ToString();sysIface=ni.Name+((ni.NetworkInterfaceType==System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211||ni.Name.StartsWith("wl"))?" · Wi-Fi":ni.NetworkInterfaceType==System.Net.NetworkInformation.NetworkInterfaceType.Ethernet?" · Ethernet":"");
     var mac=ni.GetPhysicalAddress().GetAddressBytes();if(mac.Length>0){var sb=new System.Text.StringBuilder();for(int i=0;i<mac.Length;i++){if(i>0)sb.Append(':');sb.Append(mac[i].ToString("X2"));}sysMac=sb.ToString();}
     return;}}}catch{}
  if(sysIp=="—")try{using(var s=new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,System.Net.Sockets.SocketType.Dgram,System.Net.Sockets.ProtocolType.Udp)){s.Connect("192.168.1.1",65530);sysIp=((System.Net.IPEndPoint)s.LocalEndPoint).Address.ToString();}}catch{}
 }
 void CollectPartitions()
 {
  var want=new[]{"/","/opt","/tmp","/mnt/systemrw"};var label=new[]{"Système (/)","Données (/opt)","Temporaire (/tmp)","Système RW"};
  System.IO.DriveInfo[] drives;try{drives=System.IO.DriveInfo.GetDrives();}catch{return;}
  for(int w=0;w<want.Length;w++)foreach(var d in drives){try{if(d.Name!=want[w]||!d.IsReady||d.TotalSize<=0)continue;long used=d.TotalSize-d.AvailableFreeSpace;sysParts.Add(new[]{label[w],FmtSize(used),FmtSize(d.TotalSize),Math.Round(100.0*used/d.TotalSize).ToString(CultureInfo.InvariantCulture)});break;}catch{}}
 }
 string FmtSize(long bytes){double g=bytes/1073741824.0;if(g>=1)return g.ToString(g>=10?"0":"0.0",CultureInfo.InvariantCulture)+" Go";return Math.Round(bytes/1048576.0).ToString(CultureInfo.InvariantCulture)+" Mo";}
 Color UsageColor(double pct){return pct>=90?new Color(.89f,.22f,.17f,1):pct>=70?new Color(.96f,.57f,.11f,1):new Color(.22f,.66f,.22f,1);}
 View UsageBar(View parent,int x,int y,int w,double pct,out View fill)
 {
  parent.Add(new View{Position2D=new Position2D(x,y),Size2D=new Size2D(w,16),CornerRadius=new Vector4(8,8,8,8),BackgroundColor=Sf(.88f,.9f,.94f,1)});
  fill=new View{Position2D=new Position2D(x,y),Size2D=new Size2D((int)(w*Math.Max(0,Math.Min(100,pct))/100),16),CornerRadius=new Vector4(8,8,8,8),BackgroundColor=UsageColor(pct)};parent.Add(fill);return fill;
 }
 TextLabel InfoRow(View panel,int y,string key,string val)
 {
  panel.Add(Label(key,36,y,284,36,22,muted,"Medium"));
  var v=Label(val,326,y,518,36,22,ink,"Semibold");panel.Add(v);return v;
 }
 // ---- update guard: keep the firmware and app auto-updates off (an update could replace BelAir OS or close the root access) ----
 const string fwAutoKey="db/menu/support/softwareupdate/autoupdate",appsAutoKey="db/apps/auto_update";
 void UpdateGuard()
 {
  lastGuard=DateTime.Now;var notes=new List<string>();bool fwOff=false,appsOff=false;
  try{
   int fw;if(vconf_get_int(fwAutoKey,out fw)==0){
    if(fw!=0){bool done=false;try{done=CSharpSetItemValue("autoupdate","0");}catch{}int again;if(!(vconf_get_int(fwAutoKey,out again)==0&&again==0))done=vconf_set_int(fwAutoKey,0)==0;notes.Add("firmware réactivé → "+(done?"recoupé":"échec"));}
    fwOff=vconf_get_int(fwAutoKey,out fw)==0&&fw==0;
   }else notes.Add("firmware illisible");
  }catch(Exception ex){notes.Add("firmware: "+ex.GetType().Name);}
  try{
   int apps;if(vconf_get_bool(appsAutoKey,out apps)==0){
    if(apps!=0){bool done=vconf_set_bool(appsAutoKey,0)==0;notes.Add("applis réactivées → "+(done?"recoupé":"échec"));}
    appsOff=vconf_get_bool(appsAutoKey,out apps)==0&&apps==0;
   }else notes.Add("applis illisible");
  }catch(Exception ex){notes.Add("applis: "+ex.GetType().Name);}
  guardOk=fwOff&&appsOff;
  guardStatus=(guardOk?"Désactivées (TV et applis)":"⚠ "+(fwOff?"":"TV ")+(appsOff?"":"applis ")+"actives")+" · vérifié à "+lastGuard.ToString("HH:mm");
  try{
   string line=lastGuard.ToString("yyyy-MM-dd HH:mm:ss")+" firmware="+(fwOff?"off":"ON")+" apps="+(appsOff?"off":"ON")+(notes.Count>0?" · "+string.Join(" · ",notes):"");
   string path="/tmp/quiet-update-guard.txt";var lines=File.Exists(path)?new List<string>(File.ReadAllLines(path)):new List<string>();lines.Add(line);
   File.WriteAllLines(path,lines.Count>30?lines.GetRange(lines.Count-30,30):lines);
  }catch{}
  if(page==6&&tabs!=null&&notes.Count>0)BuildPage();
 }
 void BuildSystemInfo()
 {
  CollectSystemInfo();
  var L=Panel(80,250,880,548,Sf(1,1,1,.96f),24);
  L.Add(Label("Appareil & réseau",36,22,800,40,27,ink,"Semibold"));
  int ly=92,st=56;
  InfoRow(L,ly,"Nom d’hôte",sysHost);ly+=st;
  InfoRow(L,ly,"Système",sysOs);ly+=st;
  InfoRow(L,ly,"Build logiciel",sysBuild);ly+=st;
  InfoRow(L,ly,"Noyau",sysKernel);ly+=st;
  InfoRow(L,ly,"Adresse IP",sysIp);ly+=st;
  InfoRow(L,ly,"Interface",sysIface);ly+=st;
  InfoRow(L,ly,"Adresse MAC",sysMac);ly+=st;
  sysUptimeVal=InfoRow(L,ly,"Démarré depuis",sysUptime);
  var C=Panel(990,250,850,256,Sf(1,1,1,.96f),24);
  C.Add(Label("Processeur",36,22,500,40,27,ink,"Semibold"));
  C.Add(Label(sysCpuModel,36,74,780,32,21,muted,"Medium"));
  C.Add(Label("Utilisation",36,124,300,40,23,muted,"Medium"));
  sysCpuVal=Label(sysCpuPct,470,118,344,48,32,ink,"Semibold");sysCpuVal.HorizontalAlignment=HorizontalAlignment.End;C.Add(sysCpuVal);
  UsageBar(C,36,176,778,memTotalKb>0?0:0,out sysCpuFill);sysCpuFill.Size2D=new Size2D(0,16);
  C.Add(Label("Charge moyenne",36,206,300,34,21,muted,"Medium"));
  sysLoadVal=Label(sysLoad,470,204,344,34,21,ink,"Semibold");sysLoadVal.HorizontalAlignment=HorizontalAlignment.End;C.Add(sysLoadVal);
  var M=Panel(990,526,850,272,Sf(1,1,1,.96f),24);
  M.Add(Label("Mémoire vive",36,22,500,40,27,ink,"Semibold"));
  sysMemVal=Label("",36,80,778,40,24,ink,"Semibold");M.Add(sysMemVal);
  UsageBar(M,36,142,778,memPct,out sysMemFill);
  M.Add(Label("Utilisée = total − disponible",36,176,778,32,20,muted,"Medium"));
  M.Add(Label("Mises à jour automatiques",36,220,330,32,21,muted,"Medium"));
  var gs=Label(guardStatus,360,220,454,32,21,guardOk?new Color(.13f,.55f,.3f,1):new Color(.85f,.25f,.2f,1),"Semibold");gs.HorizontalAlignment=HorizontalAlignment.End;M.Add(gs);
  var S=Panel(80,818,1760,208,Sf(1,1,1,.96f),24);
  S.Add(Label("Stockage",36,20,500,40,27,ink,"Semibold"));
  if(sysParts.Count==0)S.Add(Label("Informations de stockage indisponibles.",36,96,900,36,22,muted,"Medium"));
  for(int i=0;i<sysParts.Count&&i<4;i++){var p=sysParts[i];int col=i%2,row=i/2,cx=36+col*878,cy=76+row*66;
   S.Add(Label(p[0],cx,cy,420,32,22,ink,"Semibold"));
   var val=Label(p[1]+" / "+p[2]+"  ("+p[3]+" %)",cx,cy,820,32,21,muted,"Medium");val.HorizontalAlignment=HorizontalAlignment.End;S.Add(val);
   double pp;double.TryParse(p[3],NumberStyles.Float,CultureInfo.InvariantCulture,out pp);View f;UsageBar(S,cx,cy+38,820,pp,out f);}
  Text("OK pour actualiser · Retour pour fermer",80,1032,900,34,20,muted);
  UpdateSystemInfo();
  StartSystemTimer();
 }
 void UpdateSystemInfo()
 {
  if(sysCpuVal==null)return;
  sysCpuVal.Text=sysCpuPct;sysLoadVal.Text=sysLoad;sysUptimeVal.Text=sysUptime;
  double cp;if(sysCpuPct.Length>2&&double.TryParse(sysCpuPct.Replace(" %",""),NumberStyles.Float,CultureInfo.InvariantCulture,out cp)){sysCpuFill.Size2D=new Size2D((int)(778*Math.Max(0,Math.Min(100,cp))/100),16);sysCpuFill.BackgroundColor=UsageColor(cp);}
  sysMemVal.Text=memTotalKb>0?FmtSize(memUsedKb*1024)+" / "+FmtSize(memTotalKb*1024)+"  ("+Math.Round(memPct).ToString(CultureInfo.InvariantCulture)+" %)":"—";
  if(sysMemFill!=null){sysMemFill.Size2D=new Size2D((int)(778*Math.Max(0,Math.Min(100,memPct))/100),16);sysMemFill.BackgroundColor=UsageColor(memPct);}
 }
 void StartSystemTimer()
 {
  if(systemTimer!=null){systemTimer.Stop();systemTimer.Dispose();systemTimer=null;}
  systemTimer=new Tizen.NUI.Timer(1500);
  systemTimer.Tick+=(s,e)=>{if(page!=6)return false;CollectSystemInfo();UpdateSystemInfo();return true;};
  systemTimer.Start();
 }
 void BuildWeatherDetail()
 {
  bool hourly=selectedForecastHour>=0&&selectedForecastHour<forecastHours.Count;
  bool daily=selectedForecastDay>=0&&selectedForecastDay<forecast.Count;
  if(!hourly&&!daily)return;
  string heading,title,temperature,condition;
  int code;string[] labels,values;
  var dayHours=new List<ForecastHour>();
  if(hourly){
   var hour=forecastHours[selectedForecastHour];heading="Détail horaire";title=hour.Time;temperature=hour.Temperature;condition=WeatherDescription(hour.Code);code=hour.Code;
   labels=new[]{"Ressenti","Probabilité de pluie","Vent"};values=new[]{hour.FeelsLike,hour.Precipitation,hour.Wind+" km/h"};
  }else{
   var day=forecast[selectedForecastDay];DateTime date;bool parsed=DateTime.TryParseExact(day.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date);
   heading="Détail sur 7 jours";title=parsed?frDays[(int)date.DayOfWeek]+" "+date.Day+" "+frMonths[date.Month-1]:day.Date;temperature="Max "+day.High;condition=day.Condition;code=day.Code;
   labels=new[]{"Température maximale","Température minimale","Prévision"};values=new[]{day.High,day.Low,day.Condition};
   foreach(var h in forecastHours)if(h.Day==day.Date){dayHours.Add(h);if(dayHours.Count==24)break;}
  }
  const int cols=8,gridTop=408,rowH=92,panelW=960;
  int rows=dayHours.Count>0?(dayHours.Count+cols-1)/cols:0;
  int panelH=rows>0?gridTop+rows*rowH+52:500;
  int panelX=(1920-panelW)/2,panelY=(1080-panelH)/2;
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.04f,.06f,.1f,.62f)});
  var panel=Panel(panelX,panelY,panelW,panelH,Sf(.99f,.99f,1,1),30);
  panel.Add(Label(heading,52,30,500,42,28,muted,"Semibold"));
  panel.Add(Label(title,52,76,800,48,38,ink,"Display"));
  AnimateWeatherIcon(AddWeatherIcon(panel,54,138,code));
  panel.Add(Label(temperature,162,135,350,66,54,ink,"Display"));
  panel.Add(Label(condition,164,198,700,38,25,ink,"Semibold"));
  int[] xs={52,356,660};
  for(int i=0;i<labels.Length;i++){
   var stat=new View{Position2D=new Position2D(xs[i],248),Size2D=new Size2D(280,112),BackgroundColor=Sf(.94f,.95f,.98f,1),CornerRadius=new Vector4(18,18,18,18)};
   panel.Add(stat);
   stat.Add(Label(labels[i],18,16,244,28,17,muted,"Semibold"));
   stat.Add(Label(values[i],18,48,244,48,30,ink,"Display"));
  }
  int hintY=445;
  if(rows>0){
   panel.Add(Label("Par heure",52,372,400,32,22,ink,"Semibold"));
   int gap=(panelW-104-cols*100)/(cols-1);
   for(int i=0;i<dayHours.Count;i++){
    var h=dayHours[i];int r=i/cols,c=i%cols;
    var cell=new View{Position2D=new Position2D(52+c*(100+gap),gridTop+r*rowH),Size2D=new Size2D(100,80),BackgroundColor=Sf(.94f,.95f,.98f,1),CornerRadius=new Vector4(16,16,16,16)};
    panel.Add(cell);
    var t=Label((h.Time!=null&&h.Time.Length>=2?h.Time.Substring(0,2):h.Time)+"h",0,8,100,26,17,muted,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;cell.Add(t);
    var tv=Label(h.Temperature,0,30,100,32,24,ink,"Display");tv.HorizontalAlignment=HorizontalAlignment.Center;cell.Add(tv);
    var rv=Label("pluie "+h.Precipitation,0,60,100,20,14,muted,"Medium");rv.HorizontalAlignment=HorizontalAlignment.Center;cell.Add(rv);
   }
   hintY=gridTop+rows*rowH+14;
  }
  panel.Add(Label("OK ou Retour pour fermer",52,hintY,856,30,18,muted));
 }
 void BuildLibrary()
 {
  if(library.Count==0){Text("Impossible de charger les applications.",80,300,1500,70,32,ink);return;}
  libraryPage=Math.Clamp(libraryPage,0,AppLibrary.PageCount(library.Count)-1);
  int start=libraryPage*AppLibrary.PageSize,count=AppLibrary.VisibleCount(library.Count,libraryPage);
  for(int i=0;i<count;i++)
  {
   var app=library[start+i];int x=80+(i%5)*358,y=266+(i/5)*245,w=328,h=184;
   var shadow=Panel(x-5,y+7,w+10,h+8,new Color(.12f,.15f,.25f,.08f),22);
   var card=Panel(x+w/2,y+h/2,w,h,HexColor(app.Background),18);
   card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
   AddAppArtwork(card,app,w,h);
   card.Add(new ImageView{ResourceUrl=Root+"card-glass.svg",Size2D=new Size2D(w,h)});
   var ring=new View{Size2D=new Size2D(w,h),BackgroundColor=new Color(0,0,0,0),CornerRadius=new Vector4(18,18,18,18),BorderlineWidth=4,BorderlineColor=accent,Opacity=0};
   card.Add(ring);
   var label=Text(app.Name,x,y+h+14,w,38,27,ink,"Semibold");
   slots.Add(new Slot{Card=card,Shadow=shadow,FocusRing=ring,Label=label,Name=app.Name,Action="app:"+app.Id,Description=app.Description,Row=i/5,X=x,Y=y,W=w,H=h});
  }
  Text(library.FindAll(a=>!a.Utility).Count+" applis",80,218,500,30,21,muted);
  if(AppLibrary.PageCount(library.Count)>1){
   var pages=Text((libraryPage+1)+" / "+AppLibrary.PageCount(library.Count),1650,218,190,30,21,muted);
   pages.HorizontalAlignment=HorizontalAlignment.End;
  }
 }
 Color HexColor(string hex)
 {
  try{int n=Convert.ToInt32(hex.TrimStart('#'),16);return new Color(((n>>16)&255)/255f,((n>>8)&255)/255f,(n&255)/255f,1);}
  catch{return white;}
 }
 Color TileTitleColor(string hex)
 {
  try{int n=Convert.ToInt32(hex.TrimStart('#'),16);double luminance=.299*((n>>16)&255)+.587*((n>>8)&255)+.114*(n&255);return luminance>155?inkFixed:white;}
  catch{return white;}
 }
 void AddAppArtwork(View card,LibraryApp app,int w,int h)
 {
  string dl=app.Utility?null:DockLogo(app.Id);
  if(dl!=null){
   card.BackgroundColor=LogoTileBg();AddThemedLogo(card,dl,w,h);
   return;
  }
  string ai=app.Utility?null:AppIconFile(app.Id);
  if(ai!=null){
   if(AppIconCover(app.Id))
    card.Add(new ImageView{Size2D=new Size2D(w,h),FittingMode=FittingModeType.ScaleToFill,SamplingMode=SamplingModeType.BoxThenLanczos,PixelArea=new RelativeVector4(0f,0.2195f,1f,0.561f),DesiredWidth=512,DesiredHeight=512,ResourceUrl=Root+ai,CornerRadius=new Vector4(18,18,18,18)});
   else{
    card.BackgroundColor=AppIconBg(app.Id);
    int s=app.Id=="QY16tZ64E3.francetv"?150:176;
    card.Add(new ImageView{Position2D=new Position2D((w-s)/2,(h-s)/2),Size2D=new Size2D(s,s),FittingMode=FittingModeType.ShrinkToFit,SamplingMode=SamplingModeType.BoxThenLanczos,DesiredWidth=s*2,DesiredHeight=s*2,ResourceUrl=Root+ai});
   }
   return;
  }
  if(!app.Utility){
   var cached=GetArtwork(app,w,h);
   if(cached.Image.LoadingStatus!=ImageView.LoadingStatusType.Ready)pageArtworkCold++;
   cached.Parent.Remove(cached.Image);card.Add(cached.Image);cached.Parent=card;
   float size=Math.Min(38f,Math.Min(h*.30f,(w-32f)/(Math.Max(1,app.Name.Length)*.58f)));
   var title=Label(app.Name,12,0,w-24,h,size,TileTitleColor(app.Background),"Semibold");
   title.HorizontalAlignment=HorizontalAlignment.Center;card.Add(title);
  }
  if(app.Utility){
   card.Add(new View{Position2D=new Position2D(w/2-28,h/2-2),Size2D=new Size2D(56,4),BackgroundColor=inkFixed});
   card.Add(new View{Position2D=new Position2D(w/2-2,h/2-28),Size2D=new Size2D(4,56),BackgroundColor=inkFixed});
  }
 }
 static string DockLogo(string id)
 {
  switch(id){
   case "org.tizen.netflix-app":return "netflix";
   case "com.samsung.tv.cobalt-yt":return "youtube";
   case "org.tizen.primevideo":return "prime";
   case "kIciSQlYEM.plex":return "plex";
   case "QmpwflxWdt.CanalPlusLauncher":return "canal-plus";
   case "com.samsung.tv.aria-video":return "apple-tv";
  }
  return null;
 }
 static string AppIconFile(string id)
 {
  switch(id){
   case "MCmYXNxgcu.DisneyPlus":return "appicon-disney.png";
   case "znCs95n93Q.OqeeTV":return "appicon-oqee.png";
   case "QY16tZ64E3.francetv":return "appicon-france-tv.png";
   case "aLlph042JZ.arte":return "appicon-arte.png";
   case "WG2lklBRNT.SmartTVNextGen":return "appicon-pathe.png";
   case "com.samsung.tv.store":return "appicon-samsung-store.png";
  }
  return null;
 }
 static bool AppIconCover(string id){return id=="MCmYXNxgcu.DisneyPlus";}
 Color AppIconBg(string id)
 {
  switch(id){
   case "znCs95n93Q.OqeeTV":return HexColor("#FFFFFF");
   case "aLlph042JZ.arte":return HexColor("#FA481C");
   case "WG2lklBRNT.SmartTVNextGen":return HexColor("#FFC426");
   case "com.samsung.tv.store":return HexColor("#3C3C3C");
   case "QY16tZ64E3.francetv":return HexColor("#191E25");
  }
  return white;
 }
 void PreloadArtwork()
 {
  // Keep the real image views alive on the scene; moving between pages never
  // destroys their decoded textures. The zero-opacity parent is never drawn.
  artworkPreloadRoot=new View{Size2D=new Size2D(1920,1080),Opacity=0};
  Window.Default.Add(artworkPreloadRoot);
  foreach(var app in library)if(!app.Utility&&DockLogo(app.Id)==null&&AppIconFile(app.Id)==null)GetArtwork(app,328,184);
 }
 CachedArtwork GetArtwork(LibraryApp app,int w,int h)
 {
  string key=app.Id+"@"+w+"x"+h;
  if(artworkCache.TryGetValue(key,out var cached))return cached;
  bool cover=app.Fit=="cover",full=cover||app.Fit=="contain-full";
  int iw=full?w:w-54,ih=full?h:h-40;var crop=app.Crop;
  float cropWidth=crop==null?1:crop[2],cropHeight=crop==null?1:crop[3];
  var image=new ImageView{
   Position2D=new Position2D((w-iw)/2,(h-ih)/2),Size2D=new Size2D(iw,ih),
   FittingMode=cover?FittingModeType.ScaleToFill:FittingModeType.ShrinkToFit,
   SamplingMode=SamplingModeType.BoxThenLanczos,
   DesiredWidth=(int)Math.Ceiling(iw*2/cropWidth),DesiredHeight=(int)Math.Ceiling(ih*2/cropHeight),
   ReleasePolicy=ReleasePolicyType.Destroyed,CornerRadius=new Vector4(18,18,18,18)};
  if(crop!=null)image.PixelArea=new RelativeVector4(crop[0],crop[1],crop[2],crop[3]);
  cached=new CachedArtwork{Image=image,Parent=artworkPreloadRoot};
  artworkCache.Add(key,cached);artworkViewsCreated++;
  image.ResourceReady+=(s,e)=>WriteState("artwork_ready");
  artworkPreloadRoot.Add(image);image.ResourceUrl=Root+app.Asset;
  return cached;
 }
 void ParkArtwork()
 {
  foreach(var cached in artworkCache.Values)if(cached.Parent!=artworkPreloadRoot){
   cached.Parent.Remove(cached.Image);artworkPreloadRoot.Add(cached.Image);cached.Parent=artworkPreloadRoot;
  }
 }
 int ArtworkReadyCount()
 {
  int count=0;foreach(var cached in artworkCache.Values)
   if(cached.Image.LoadingStatus==ImageView.LoadingStatusType.Ready)count++;
  return count;
 }
 string ArtworkPending()
 {
  var pending=new List<string>();
  foreach(var entry in artworkCache)
   if(entry.Value.Image.LoadingStatus!=ImageView.LoadingStatusType.Ready)
    pending.Add(entry.Key+":"+entry.Value.Image.LoadingStatus);
  return string.Join(";",pending);
 }
 void SettingsTile(string name,string action,string description,string icon,int column,int row)
 {
  int x=80+column*450,y=256+row*306,w=410,h=286;
  var card=Panel(x+w/2,y+h/2,w,h,new Color(0,0,0,0),24);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
  card.Add(new ImageView{ResourceUrl=Root+"quick-"+icon+".svg",Size2D=new Size2D(w,h)});
  card.Add(Label(name,26,25,348,42,29,white,"Semibold"));
  var value=Label("",26,151,358,56,38,white,"Display");card.Add(value);
  card.Add(Label(description,26,226,370,34,20,new Color(.95f,.96f,1,1)));
  slots.Add(new Slot{Card=card,Label=value,Name=name,Action=action,Description=description,Row=row,X=x,Y=y,W=w,H=h});
 }
 void BuildMarketTile()
 {
  int x=InfoX(2),y=infoY,w=infoW,h=infoH;
  var card=InfoTile(2);
  card.Add(Label("CAC 40",22,14,140,28,18,muted,"Semibold"));
  marketTime=Label("",150,14,w-172,28,17,muted,"Medium");marketTime.HorizontalAlignment=HorizontalAlignment.End;card.Add(marketTime);
  marketValue=Label("",20,44,w-40,58,46,ink,"Display");card.Add(marketValue);
  marketChange=Label("",22,102,170,30,21,ink,"Semibold");card.Add(marketChange);
  marketChartHost=new View{Position2D=new Position2D(186,100),Size2D=new Size2D(120,40)};card.Add(marketChartHost);
  marketSub=Label("",22,146,w-44,26,18,muted,"Medium");card.Add(marketSub);
  marketSlotIndex=slots.Count;
  slots.Add(new Slot{Card=card,Name="Bourse · CAC 40",Action="markets",Description="OK pour les cours et l’actualité financière",Row=-2,X=x,Y=y,W=w,H=h});
  UpdateMarketTile();
 }
 // ---- markets ----
 MarketQuote Quote(string symbol){foreach(var q in marketQuotes)if(q.Symbol==symbol)return q;return null;}
 static string Grp(string s){return s.Replace('\u202F',' ').Replace('\u00A0',' ');}
 string FmtPrice(MarketQuote q){return Grp(q.Price.ToString(q.Price<20?"N4":"N2",frCulture));}
 double ChangePct(MarketQuote q){return q.Previous>0?(q.Price-q.Previous)/q.Previous*100:0;}
 string FmtPct(double p){return (p>=0?"▲ +":"▼ −")+Math.Abs(p).ToString("0.00",frCulture)+" %";}
 Color PctColor(double p,bool onDark){return p>=0?(onDark?new Color(.47f,.93f,.66f,1):new Color(.10f,.55f,.30f,1)):(onDark?new Color(1f,.55f,.52f,1):new Color(.78f,.16f,.13f,1));}
 string QuoteTime(MarketQuote q){
  if(q.Time<=0)return "";var t=DateTimeOffset.FromUnixTimeSeconds(q.Time).ToLocalTime();
  return t.Date==DateTime.Today?"à "+t.ToString("HH:mm"):"clôture "+frDays[(int)t.DayOfWeek].Substring(0,3)+". "+t.Day;
 }
 // Draws the intraday line as an SVG in Home's own data folder (new file name each time so the image cache never serves a stale chart).
 string SparkFile(MarketQuote q,int w,int h,string stroke,string baseline)
 {
  if(q.Points.Count<2)return "";
  try{
   string dir=System.IO.Path.Combine(Current.DirectoryInfo.Data,"spark");Directory.CreateDirectory(dir);
   string prefix=q.Symbol.Replace("^","").Replace("=","")+"-"+w+"-";
   double min=q.Points[0],max=q.Points[0];foreach(var v in q.Points){if(v<min)min=v;if(v>max)max=v;}
   if(q.Previous>0){min=Math.Min(min,q.Previous);max=Math.Max(max,q.Previous);}
   if(max-min<1e-9)max=min+1;
   var inv=CultureInfo.InvariantCulture;int n=q.Points.Count;
   Func<int,double> X=i=>3+(w-6)*(double)i/(n-1);Func<double,double> Y=v=>5+(h-10)*(max-v)/(max-min);
   var line=new System.Text.StringBuilder();
   for(int i=0;i<n;i++)line.Append(i==0?"M":"L").Append(X(i).ToString("0.#",inv)).Append(',').Append(Y(q.Points[i]).ToString("0.#",inv));
   string area=line+"L"+X(n-1).ToString("0.#",inv)+","+h+"L3,"+h+"Z";
   string by=q.Previous>0?Y(q.Previous).ToString("0.#",inv):"-10";
   string svg="<svg xmlns=\"http://www.w3.org/2000/svg\" width=\""+w+"\" height=\""+h+"\" viewBox=\"0 0 "+w+" "+h+"\"><defs><linearGradient id=\"g\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\""+stroke+"\" stop-opacity=\".38\"/><stop offset=\"1\" stop-color=\""+stroke+"\" stop-opacity=\"0\"/></linearGradient></defs>"
    +"<path d=\""+area+"\" fill=\"url(#g)\"/><path d=\"M3,"+by+"H"+(w-3)+"\" stroke=\""+baseline+"\" stroke-opacity=\".45\" stroke-width=\"1.5\" stroke-dasharray=\"5 5\"/>"
    +"<path d=\""+line+"\" fill=\"none\" stroke=\""+stroke+"\" stroke-width=\"3\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/></svg>";
   string file=System.IO.Path.Combine(dir,prefix+DateTime.Now.Ticks+".svg");File.WriteAllText(file,svg);
   foreach(var old in Directory.GetFiles(dir,prefix+"*.svg"))if(old!=file)try{File.Delete(old);}catch{}
   return file;
  }catch{return "";}
 }
 void SetChart(View host,MarketQuote q,string stroke,string baseline)
 {
  if(host==null)return;
  while(host.ChildCount>0){var child=host.GetChildAt(0);host.Remove(child);child.Dispose();}
  string file=q==null?"":SparkFile(q,host.Size2D.Width,host.Size2D.Height,stroke,baseline);
  if(file!="")host.Add(new ImageView{ResourceUrl=file,Size2D=new Size2D(host.Size2D.Width,host.Size2D.Height)});
 }
 void UpdateMarketTile()
 {
  if(marketValue==null)return;
  var cac=Quote("^FCHI");
  if(cac==null){marketValue.Text="—";marketChange.Text="Chargement…";marketChange.TextColor=muted;marketTime.Text="";marketSub.Text="";SetChart(marketChartHost,null,"","");return;}
  double p=ChangePct(cac);
  marketValue.Text=FmtPrice(cac);marketChange.Text=FmtPct(p);marketChange.TextColor=PctColor(p,dark);marketTime.Text=QuoteTime(cac);
  var parts=new List<string>();foreach(var sym in new[]{"^STOXX50E","^GSPC"}){var q=Quote(sym);if(q!=null)parts.Add((sym=="^GSPC"?"S&P":"Stoxx")+" "+(ChangePct(q)>=0?"+":"−")+Math.Abs(ChangePct(q)).ToString("0.00",frCulture)+" %");}
  marketSub.Text=string.Join(" · ",parts);
  SetChart(marketChartHost,cac,p>=0?(dark?"#8CF1BF":"#1E9E5A"):(dark?"#FF8A80":"#D33A2C"),dark?"#C9FFE0":"#8A94A6");
 }
 void ReceiveMarkets(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){
   var list=new List<MarketQuote>();
   foreach(var el in doc.RootElement.EnumerateArray()){
    var q=new MarketQuote{Symbol=el.GetProperty("Symbol").GetString()??"",Name=el.GetProperty("Name").GetString()??"",Price=el.GetProperty("Price").GetDouble(),Previous=el.GetProperty("Previous").GetDouble(),Time=el.GetProperty("Time").GetInt64()};
    foreach(var v in el.GetProperty("Points").EnumerateArray())q.Points.Add(v.GetDouble());
    if(el.TryGetProperty("Times",out var ts)&&ts.ValueKind==JsonValueKind.Array)foreach(var v in ts.EnumerateArray())q.Times.Add(v.GetInt64());
    list.Add(q);
   }
   if(list.Count>0){marketQuotes.Clear();marketQuotes.AddRange(list);}
  }}catch{}
  UpdateMarketTile();
  if(page==0&&newsMenuOpen&&!newsDetailOpen&&newsCategoryIndex==3){int previousArea=area;BuildPage();area=previousArea;if(area==8&&NewsItems(3).Count>0)selected=newsArticleSlotStart+Math.Min(newsArticleIndex,ArticleLimit()-1);else if(area==11&&quoteSlotStart>=0)selected=quoteSlotStart+Math.Min(quoteIndex,QuoteCount()-1);else{area=7;selected=newsCategorySlotStart+newsCategoryIndex;}UpdateFocus(false);}
  WriteState("markets_received:"+marketQuotes.Count);
 }
 void BuildFactTile()
 {
  int x=InfoX(3),y=infoY,w=infoW,h=infoH;
  var card=InfoTile(3);
  card.Add(Label("LE SAVIEZ-VOUS ?",22,14,w-44,28,18,muted,"Semibold"));
  factBody=Label(facts[factIndex],22,46,w-44,124,19,ink,"Medium");factBody.MultiLine=true;factBody.LineWrapMode=LineWrapMode.Word;factBody.Ellipsis=true;factBody.VerticalAlignment=VerticalAlignment.Top;card.Add(factBody);
  factSlotIndex=slots.Count;
  slots.Add(new Slot{Card=card,Name="Le saviez-vous ?",Action="fact",Description="OK pour lire l’anecdote en entier",Row=-2,X=x,Y=y,W=w,H=h});
 }
 void BuildFactDetail()
 {
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.03f,.05f,.09f,.72f)});
  var panel=Panel(330,196,1260,688,Sf(.99f,.99f,1,1),30);
  panel.Add(Label("LE SAVIEZ-VOUS ?",40,30,1180,40,21,muted,"Semibold"));
  var body=Label(facts[factIndex],40,104,1180,440,30,ink,"Medium");
  body.MultiLine=true;body.LineWrapMode=LineWrapMode.Word;body.Ellipsis=false;body.VerticalAlignment=VerticalAlignment.Top;panel.Add(body);
  panel.Add(Label("OK · fait suivant",40,590,1180,30,19,accent,"Semibold"));
  panel.Add(Label("Retour · accueil",40,628,1180,26,17,muted,"Medium"));
 }
 // Anecdotes from French Wikipedia's "Le saviez-vous ?" (current ones plus archives), fetched daily by the helper.
 void ReceiveFacts(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){var list=new List<string>();foreach(var f in doc.RootElement.EnumerateArray()){string t=f.GetString()??"";if(t.Length>=30)list.Add(t);}
   if(list.Count>=10){facts=list;factIndex=Math.Min(factIndex,facts.Count-1);RotateFact();}}}catch{}
  WriteState("facts_received:"+facts.Count);
 }
 void RotateFact()
 {
  if(facts.Count<2)return;
  var random=new Random(Environment.TickCount+factIndex);
  int next=factIndex;
  while(next==factIndex)next=random.Next(facts.Count);
  factIndex=next;lastFactUpdate=DateTime.Now;
  if(factDetailOpen)BuildPage();else if(factBody!=null)factBody.Text=facts[factIndex];
  WriteState("fact_rotated:"+factIndex);
 }
 Slot Feature(string label,string action,string description,string asset,int x,int y,int w,int h,string shape)
 {
  var shadow=Add(new ImageView{ResourceUrl=Root+"shadow-"+shape+".svg",Position2D=new Position2D(x-24,y-10),Size2D=new Size2D(w+48,h+64)});
  var card=Panel(x+w/2,y+h/2,w,h,new Color(0,0,0,0),18);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
  card.Add(new ImageView{ResourceUrl=Root+asset,Size2D=new Size2D(w,h)});
  int captionY=y+h+(shape=="hero"?30:14);float captionSize=w<300?25:31;float descriptionSize=w<300?16:24;
  var caption=Text(label,x,captionY,w,38,captionSize,ink,"Semibold");
  Text(description,x,captionY+40,w,36,descriptionSize,muted);
  var slot=new Slot{Card=card,Shadow=shadow,Label=caption,Name=label,Action=action,Description=description,Row=0,X=x,Y=y,W=w,H=h};slots.Add(slot);return slot;
 }
 // The dock's glass rim drawn at the tile's exact size (a scaled 244 px drawing left a second inner frame).
 string GlassFile(int w,int h)
 {
  var inv=CultureInfo.InvariantCulture;
  string svg="<svg xmlns=\"http://www.w3.org/2000/svg\" width=\""+w+"\" height=\""+h+"\" viewBox=\"0 0 "+w+" "+h+"\"><defs><linearGradient id=\"s\" x2=\"0\" y2=\"1\"><stop stop-color=\"#fff\" stop-opacity=\".20\"/><stop offset=\".42\" stop-color=\"#fff\" stop-opacity=\".045\"/><stop offset=\"1\" stop-color=\"#fff\" stop-opacity=\"0\"/></linearGradient></defs>"
   +"<rect x=\"1\" y=\"1\" width=\""+(w-2)+"\" height=\""+(h-2)+"\" rx=\"21\" fill=\"url(#s)\"/>"
   +"<rect x=\"1.25\" y=\"1.25\" width=\""+(w-2.5).ToString(inv)+"\" height=\""+(h-2.5).ToString(inv)+"\" rx=\"20.75\" fill=\"none\" stroke=\"#fff\" stroke-opacity=\".40\" stroke-width=\"1.5\"/>"
   +"<rect x=\".5\" y=\".5\" width=\""+(w-1)+"\" height=\""+(h-1)+"\" rx=\"21.5\" fill=\"none\" stroke=\"#000\" stroke-opacity=\".10\" stroke-width=\"1\"/></svg>";
  return WallFile("dock-glass-"+w+"x"+h,svg);
 }
 static string UsageKey(string action){if(action.StartsWith("atv:")){int c=action.IndexOf(':',4);return c>4?action.Substring(4,c-4):action;}return action.StartsWith("app:")?action.Substring(4):action;}
 // Dock shortcuts open the same app on the Apple TV (Home Assistant "select_source"), then switch the TV to its HDMI input.
 void LaunchOnAppleTv(string action)
 {
  int c=action.IndexOf(':',4);if(c<0)return;string source=action.Substring(c+1);
  BumpUsage(UsageKey(action));
  string atv="media_player.appletv_appletv",esc=source.Replace("\\","\\\\").Replace("\"","\\\"");
  string calls="[{\"p\":\"media_player/turn_on\",\"b\":{\"entity_id\":\""+atv+"\"},\"w\":0},{\"p\":\"media_player/select_source\",\"b\":{\"entity_id\":\""+atv+"\",\"source\":\""+esc+"\"},\"w\":300}]";
  IntPtr ac;int r=app_control_create(out ac);
  if(r==0){try{
   r=app_control_set_app_id(ac,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(ac,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(ac,"quiet_ha_calls_silent",calls);
   if(r==0)r=app_control_send_launch_request(ac,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(ac);}}
  if(r!=0){ShowError("Commande Apple TV indisponible.");return;}
  notice.Text=(source=="TV"?"Apple TV":source)+" sur l’Apple TV…";WriteState("atv:"+source);
  SelectSource(13);
 }
 void Dock(string label,string action,string icon,int n,int count)
 {
  int H=130,G=24,W=232,startX=96;
  int cx=startX+n*(W+G)+W/2,cy=836+H/2;
  var card=Panel(cx,cy,W,H,DockBg(icon),22);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
  if(icon=="france-2"){
   card.BackgroundColor=LogoTileBg();
   var name=Label("France 2",0,16,W,52,34,ink,"Semibold");name.HorizontalAlignment=HorizontalAlignment.Center;card.Add(name);
   var chip=new View{Position2D=new Position2D((W-132)/2,72),Size2D=new Size2D(132,30),BackgroundColor=new Color(.86f,.16f,.2f,1),CornerRadius=new Vector4(15,15,15,15)};card.Add(chip);
   chip.Add(new View{Position2D=new Position2D(16,11),Size2D=new Size2D(8,8),BackgroundColor=white,CornerRadius=new Vector4(4,4,4,4)});
   chip.Add(Label("EN DIRECT",30,0,92,30,14,white,"Semibold"));
  }
  else{card.BackgroundColor=LogoTileBg();AddThemedLogo(card,icon,W,H);}
  string glass=W==244?Root+"dock-glass.svg":GlassFile(W,H);if(glass!="")card.Add(new ImageView{ResourceUrl=glass,Size2D=new Size2D(W,H)});
  slots.Add(new Slot{Card=card,Name=label,Action=action,Description=label,Row=1,X=cx-W/2,Y=cy-H/2,W=W,H=H});
 }
 // ---- live channel on the Apple TV: CANAL+ channel page link, then Down and OK on its "direct" entry (France 2 only for now) ----
 void StartChannel(string slug)
 {
  if(slug!="france-2")return;
  BumpUsage("channel:"+slug);
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_channel",slug);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
  if(r!=0){ShowError("Commande Apple TV indisponible.");return;}
  notice.Text="France 2 en direct sur l’Apple TV…";WriteState("channel:"+slug);
  SelectSource(13);
 }
 // App logos follow the theme (like dark / light app icons): brand logo on a dark tile at night, on a white tile by day.
 // The logos are the official artwork with its background cut out; white wordmarks turn dark on the light tile.
 static readonly Dictionary<string,int[]> logoSize=new Dictionary<string,int[]>{{"youtube",new[]{353,246}},{"netflix",new[]{236,65}},{"plex",new[]{185,301}},{"canal-plus",new[]{375,70}},{"apple-tv",new[]{195,99}},{"prime",new[]{312,312}}};
 Color LogoTileBg(){return dark?new Color(.114f,.106f,.122f,1):new Color(1,1,1,1);}
 void AddThemedLogo(View card,string icon,int w,int h)
 {
  if(!logoSize.TryGetValue(icon,out var sz))return;
  bool mono=icon=="canal-plus"||icon=="apple-tv"||icon=="prime";
  double bw=w*(icon=="plex"?.5:icon=="prime"?.5:.64),bh=h*(icon=="plex"?.6:icon=="prime"?.66:icon=="youtube"?.46:icon=="apple-tv"?.44:.5);
  double k=Math.Min(bw/sz[0],bh/sz[1]);int iw=(int)(sz[0]*k),ih=(int)(sz[1]*k);
  card.Add(new ImageView{ResourceUrl=Root+"dock-"+icon+(!dark&&mono?"-gl":"-g")+".png",Position2D=new Position2D((w-iw)/2,(h-ih)/2),Size2D=new Size2D(iw,ih),SamplingMode=SamplingModeType.BoxThenLanczos,DesiredWidth=iw*2,DesiredHeight=ih*2});
 }
 Color DockBg(string icon)
 {
  switch(icon){
   case "france-2":return HexColor("#1D2230");
   case "netflix":return HexColor("#FEFEFE");
   case "youtube":return HexColor("#FFFFFF");
   case "canal-plus":return HexColor("#202328");
   case "plex":return HexColor("#1F1F1F");
   case "prime":return HexColor("#0578FF");
   default:return HexColor("#202328");
  }
 }
 // ---- BelAir OS 2 icons: small stroke drawings written once as SVG in Home's data folder ----
 static readonly Dictionary<string,string> iconPaths=new Dictionary<string,string>{
  {"sun","<circle cx='12' cy='12' r='4.5' fill='none' stroke='C' stroke-width='1.9'/><path d='M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5.3 5.3l1.4 1.4M17.3 17.3l1.4 1.4M5.3 18.7l1.4-1.4M17.3 6.7l1.4-1.4' stroke='C' stroke-width='1.9' stroke-linecap='round'/>"},
  {"moon","<path d='M20 14.5A8 8 0 1 1 9.5 4a6.5 6.5 0 0 0 10.5 10.5z' fill='none' stroke='C' stroke-width='1.9' stroke-linejoin='round'/>"},
  {"play","<path d='M8 5v14l11-7z' fill='C'/>"},
  {"pause","<rect x='6' y='5' width='4' height='14' rx='1.2' fill='C'/><rect x='14' y='5' width='4' height='14' rx='1.2' fill='C'/>"},
  {"bulb","<path d='M9 18h6M10 21h4M12 3a6 6 0 0 0-3.5 10.9V16h7v-2.1A6 6 0 0 0 12 3z' fill='none' stroke='C' stroke-width='1.9' stroke-linecap='round' stroke-linejoin='round'/>"},
  {"plug","<path d='M9 3v5M15 3v5M6 8h12v3a6 6 0 0 1-12 0zM12 17v4' fill='none' stroke='C' stroke-width='1.9' stroke-linecap='round' stroke-linejoin='round'/>"},
  {"tv","<rect x='2.5' y='4.5' width='19' height='12.5' rx='2.2' fill='none' stroke='C' stroke-width='1.9'/><path d='M8.5 20.5h7' stroke='C' stroke-width='1.9' stroke-linecap='round'/><path d='M10.2 8.3v5.2l4.3-2.6z' fill='C'/>"},
  {"film","<rect x='3.5' y='5.5' width='17' height='13' rx='2' fill='none' stroke='C' stroke-width='1.9'/><path d='M8 5.5v13M16 5.5v13' stroke='C' stroke-width='1.9'/>"},
 };
 string IconFile(string name,string hex)
 {
  try{string dir=System.IO.Path.Combine(Current.DirectoryInfo.Data,"icons2");Directory.CreateDirectory(dir);
   string f=System.IO.Path.Combine(dir,name+"-"+hex.TrimStart('#')+".svg");
   if(!File.Exists(f))File.WriteAllText(f,"<svg xmlns='http://www.w3.org/2000/svg' width='48' height='48' viewBox='0 0 24 24'>"+iconPaths[name].Replace("'C'","'"+hex+"'")+"</svg>");
   return f;}catch{return "";}
 }
 static string HexOf(Color c){return "#"+((int)(c.R*255)).ToString("X2")+((int)(c.G*255)).ToString("X2")+((int)(c.B*255)).ToString("X2");}
 static readonly string[] sceneIcon2={"sun","moon","play","pause"},sceneHint={"Réveil en douceur","Tout éteindre","Ambiance cinéma","Lumière du salon"};
 static readonly string[] sceneTint={"#F5A623","#6E6AE8","#E5484D","#12A594"};
 readonly Dictionary<string,View> coverFills=new Dictionary<string,View>();
 readonly Dictionary<string,ImageView> lampIcons=new Dictionary<string,ImageView>();
 Color LampOnTile(){return dark?new Color(1f,.74f,.30f,.18f):new Color(1f,.94f,.82f,.92f);}
 // ---- Maison (BelAir OS 2 mockup): scenes, lights as Control Center tiles lit with each lamp's real colour, shutters ----
 const int mScY=212,mScH=146,mLtY=428,mLtH=108,mLtG=18,mCvY=856,mCvH=130;
 static readonly string[] sceneIconDark={"#FFD68C","#C9C2FF","#FF8F86","#8FE3D1"};
 readonly Dictionary<string,ImageView> lampArt=new Dictionary<string,ImageView>();
 readonly Dictionary<string,string> lightRgb=new Dictionary<string,string>();
 void BuildHomeAssistant()
 {
  int bx=96,full=1728;
  lampCards.Clear();lampDots.Clear();lampNames.Clear();lampStates.Clear();coverLabels.Clear();coverFills.Clear();lampIcons.Clear();lampArt.Clear();
  Text("Maison",bx,124,330,72,56,ink,"Display");
  Text(HomeSummary(),bx+262,146,1300,40,24,muted,"Medium");
  int sg=32,sw=(full-3*sg)/4;
  for(int i=0;i<haSceneId.Length;i++){
   int x=bx+i*(sw+sg);
   var card=Panel(x+sw/2,mScY+mScH/2,sw,mScH,GlassFill(),30);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;card.BorderlineWidth=1;card.BorderlineColor=GlassEdge();
   card.Add(new ImageView{ResourceUrl=IconFile(sceneIcon2[i],dark?sceneIconDark[i]:sceneTint[i]),Position2D=new Position2D(30,(mScH-52)/2),Size2D=new Size2D(52,52)});
   card.Add(Label(haSceneName[i],104,34,sw-124,42,30,ink,"Semibold"));
   card.Add(Label(sceneHint[i],104,78,sw-124,32,20,muted,"Medium"));
   slots.Add(new Slot{Card=card,Name="Scène "+haSceneName[i],Action="ha_scene:"+haSceneId[i],Description="OK pour activer",Row=0,X=x,Y=mScY,W=sw,H=mScH});
  }
  Text("Lumières",bx,mLtY-44,600,36,26,ink,"Semibold");
  int lg=24,lw=(full-4*lg)/5;
  for(int i=0;i<haLampId.Length;i++){
   string id=haLampId[i];int col=i%5,rowN=i/5,x=bx+col*(lw+lg),y=mLtY+rowN*(mLtH+mLtG);
   var card=Panel(x+lw/2,y+mLtH/2,lw,mLtH,GlassFill(),28);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
   var dotv=new View{Position2D=new Position2D(22,(mLtH-60)/2),Size2D=new Size2D(60,60),CornerRadius=new Vector4(30,30,30,30)};card.Add(dotv);
   var art=new ImageView{Size2D=new Size2D(60,60),CornerRadius=new Vector4(30,30,30,30),Opacity=0};dotv.Add(art);lampArt[id]=art;
   var icv=new ImageView{Position2D=new Position2D(15,15),Size2D=new Size2D(30,30)};dotv.Add(icv);lampIcons[id]=icv;
   var nml=Label(haLampName[i],98,22,lw-110,34,24,ink,"Semibold");nml.Ellipsis=true;card.Add(nml);
   var stt=Label("",98,58,lw-110,28,19,muted,"Medium");stt.Ellipsis=true;card.Add(stt);
   lampCards[id]=card;lampDots[id]=dotv;lampNames[id]=nml;lampStates[id]=stt;
   slots.Add(new Slot{Card=card,Name=haLampName[i],Action="ha_lamp:"+id,Description=dimLamps.Contains(id)?"OK pour allumer ou éteindre · appui long : luminosité":colorLights.Contains(id)?"OK pour les couleurs, scènes et luminosité":"OK pour allumer ou éteindre",Row=1+rowN,X=x,Y=y,W=lw,H=mLtH});
  }
  int lampRows=(haLampId.Length+1+4)/5;
  BuildDevices(bx,lw,lg);
  foreach(var id in haLampId)SetLampVisual(id);
  Text("Volets",bx,mCvY-44,600,36,26,ink,"Semibold");
  int cg=24,cw=(full-3*cg)/4;
  for(int i=0;i<haCoverId.Length;i++){
   int x=bx+i*(cw+cg);int pos=CoverPos(haCoverId[i]);
   var card=Panel(x+cw/2,mCvY+mCvH/2,cw,mCvH,GlassFill(),28);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;card.BorderlineWidth=1;card.BorderlineColor=GlassEdge();card.ClippingMode=ClippingModeType.ClipChildren;
   var fill=new View{Size2D=new Size2D(cw,mCvH*(100-Math.Max(0,Math.Min(100,pos)))/100),BackgroundColor=dark?new Color(0,0,0,.42f):new Color(.35f,.24f,.14f,.14f)};card.Add(fill);coverFills[haCoverId[i]]=fill;
   card.Add(Label(haCoverName[i],26,24,cw-52,38,26,ink,"Semibold"));
   var val=Label(CoverText(pos),26,mCvH-56,cw-52,34,22,CoverTextColor(pos),"Semibold");card.Add(val);coverLabels[haCoverId[i]]=val;
   slots.Add(new Slot{Card=card,Label=val,Name="Volet "+haCoverName[i],Action="ha_cover:"+haCoverId[i],Description="OK pour régler l’ouverture",Row=1+lampRows,X=x,Y=mCvY,W=cw,H=mCvH});
  }
  if(!coverPopupOpen&&DateTime.Now-lastHaRequest>=TimeSpan.FromSeconds(8)){lastHaRequest=DateTime.Now;RequestHaStates();}
  if(coverPopupOpen)BuildCoverPopup();
  if(lightOpen){BuildLightPopup();if(catalogOpen)BuildSceneCatalog();}
 }
 string HomeSummary()
 {
  int on=haLampId.Count(IsLampOn);string t=SensorNum("sensor.salon_sensor_temperature","0.0");
  int closed=haCoverId.Count(c=>CoverPos(c)<100);
  return (t!=""&&t!="--"?"Salon "+t+"° · ":"")+(on==0?"tout est éteint":on==1?"1 lumière allumée":on+" lumières allumées")+" · "+(closed==0?"volets ouverts":closed==haCoverId.Length?"volets fermés":closed+" volet"+(closed>1?"s":"")+" fermé"+(closed>1?"s":""));
 }
 // What a lamp tile says under its name: brightness, Govee scene, music mode, spots lit.
 string LampStateText(string id)
 {
  if(!IsLampOn(id))return "Éteinte";
  if(id.StartsWith("switch."))return "Allumée";
  string bri=lightBri.TryGetValue(id,out var b)&&b>=0?(int)Math.Round(b*100/255.0)+" %":"";
  if(groupBulbs.TryGetValue(id,out var gb)){int n=gb.Count(x=>haLampState.TryGetValue(x,out var s)&&s=="on");return n+" sur "+gb.Length+(bri!=""?" · "+bri:"");}
  string sw=MemberEntities(id,"switch","_music_mode").First();
  if(haLampState.TryGetValue(sw,out var ms)&&ms=="on"){string sel=MemberEntities(id,"select","_music_mode").First();return "Musique"+(haSensor.TryGetValue(sel,out var mm)&&mm!=""?" · "+MusicFrench(mm):"");}
  string eff=lightEffect.TryGetValue(id,out var e)?e:"";
  if(eff!=""&&eff!="none"&&eff!="None")return "Scène "+GoveeFrench(id,eff);
  return bri!=""?bri:"Allumée";
 }
 bool LampFocused(string id){return page==5&&!lightOpen&&!coverPopupOpen&&area!=0&&selected>=0&&selected<slots.Count&&slots[selected].Action=="ha_lamp:"+id;}
 int CoverPos(string id){if(haCoverPos.TryGetValue(id,out var p))return p;return haCoverState.TryGetValue(id,out var s)&&s=="closed"?0:100;}
 string CoverText(int pos){return pos>=100?"Ouvert":pos<=0?"Fermé":"Fermé à "+(100-pos)+"%";}
 // shutter state colours readable on glass in both themes
 Color CoverTextColor(int pos){return pos>=100?(dark?new Color(.56f,.94f,.70f,1):new Color(.10f,.52f,.28f,1)):pos<=0?(dark?new Color(1f,.64f,.64f,1):new Color(.72f,.20f,.16f,1)):(dark?new Color(1f,.84f,.44f,1):new Color(.62f,.42f,.06f,1));}
 Color CoverColor2(int pos){return pos<=0?new Color(1f,.64f,.64f,1):pos>=100?new Color(.56f,.94f,.70f,1):new Color(1f,.84f,.44f,1);}
 void ChangeCover(string entity,int dir)
 {
  int pos=CoverPos(entity);pos=Math.Max(0,Math.Min(100,pos+dir*10));haCoverPos[entity]=pos;SendHaCoverPos(entity,pos);
  if(selected>=0&&selected<slots.Count&&slots[selected].Action=="ha_cover:"+entity&&slots[selected].Label!=null){slots[selected].Label.Text=CoverText(pos);slots[selected].Label.TextColor=CoverColor2(pos);}
 }
 void SendHaCoverPos(string entity,int pos)
 {
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_domain","cover");
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_service","set_cover_position");
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_entity",entity);
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_position",pos.ToString());
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
  HaCommanded(entity);
  if(r!=0)ShowError("Commande maison indisponible.");
 }
 int NearestInRow(int row,int x){int best=-1,bd=int.MaxValue;for(int i=0;i<slots.Count;i++)if(slots[i].Row==row){int dd=Math.Abs(slots[i].X-x);if(dd<bd){bd=dd;best=i;}}return best>=0?best:selected;}
 string[] GroupMembers(string id){return new[]{id};}
 bool IsLampOn(string id){foreach(var m in GroupMembers(id))if(haLampState.TryGetValue(m,out var s)&&s=="on")return true;return false;}
 void SetLampVisual(string id)
 {
  try{bool on=IsLampOn(id),f=LampFocused(id),plug=id.StartsWith("switch.");
   string eff=lightEffect.TryGetValue(id,out var e)?e:"";bool scene=on&&eff!=""&&eff!="none"&&eff!="None"&&!plug;
   string hex=on&&lightRgb.TryGetValue(id,out var rh)&&rh!=""?rh:"#FFB547";
   if(lampCards.TryGetValue(id,out var c)&&c!=null)c.BackgroundColor=f?new Color(1,1,1,dark?.94f:1f):on?(dark?new Color(1,1,1,.16f):GlassFill()):(dark?new Color(1,1,1,.06f):new Color(1,1,1,.42f));
   if(lampCards.TryGetValue(id,out var c2)&&c2!=null){c2.BorderlineWidth=f?0:1;c2.BorderlineColor=dark?new Color(1,1,1,on?.18f:.10f):new Color(0,0,0,.06f);}
   if(lampDots.TryGetValue(id,out var d)&&d!=null)d.BackgroundColor=on?HexColor(hex):dark?(f?new Color(0,0,0,.10f):new Color(1,1,1,.14f)):new Color(0,0,0,.08f);
   if(lampArt.TryGetValue(id,out var a)&&a!=null){a.Opacity=scene?1:0;if(scene){string art=SceneArt(GoveeFrench(id,eff),60,60);if(a.ResourceUrl!=art)a.ResourceUrl=art;}}
   if(lampIcons.TryGetValue(id,out var ic)&&ic!=null){ic.Opacity=f||plug?1:0;ic.ResourceUrl=IconFile(plug?"plug":"bulb",on?"#3A2500":f?"#5A5246":HexOf(muted));}
   Color nameC=f?new Color(.086f,.075f,.06f,1):on?ink:dark?new Color(1,1,1,.7f):Tx(.30f,.33f,.38f,1);
   Color stC=f?new Color(.42f,.35f,.26f,1):on?(dark?new Color(1,1,1,.7f):muted):dark?new Color(1,1,1,.5f):Tx(.5f,.53f,.58f,1);
   if(lampNames.TryGetValue(id,out var n)&&n!=null)n.TextColor=nameC;
   if(lampStates.TryGetValue(id,out var s)&&s!=null){s.Text=LampStateText(id)+(f&&!plug&&(dimLamps.Contains(id)||colorLights.Contains(id))?(colorLights.Contains(id)?" · OK : réglages":" · appui long : réglages"):"");s.TextColor=stC;}
  }catch{}
 }
 // Gradient artwork for a light scene, picked from the scene's name (Govee scenes have no pictures we may reuse).
 static readonly string[][] sceneGrad={
  new[]{"aurore|aurora|boréal","#2EE6A6","#2F6BFF","#9B3DFF"},new[]{"coucher|sunset|crépus|soir","#FF8A00","#FF2D55"},new[]{"arc-en|rainbow|fête|party|disco","#FF2D2D","#FFD500","#2EE65C","#2F6BFF"},
  new[]{"feu|fire|flamme|bougie|candle|cheminée","#FF5A00","#7A1300"},new[]{"romant|amour|love|rose|saint","#FF4FB8","#7A2CFF"},new[]{"ciné|movie|film","#1B2B52","#5A7BD8"},
  new[]{"vague|océan|ocean|mer|sea|wave|plage|eau|aqua","#00D9E8","#004A8F"},new[]{"forêt|forest|jungle|herbe|printemps|nature|vert","#2EE65C","#0B5D2A"},
  new[]{"neige|snow|hiver|glace|flocon|ice","#E4EEFF","#5A7BD8"},new[]{"nuit|night|étoile|star|lune|moon|galax","#1B1B4F","#6E6AE8"},
  new[]{"lecture|reading|travail|work|blanc|white|lumière","#FFE7C4","#FFB547"},new[]{"automne|autumn|halloween|citrouille","#FF8A00","#5A1E00"},new[]{"noël|christmas|xmas","#E5383B","#0B5D2A"}};
 static readonly string[][] sceneGradFallback={new[]{"#FF8A00","#FF2D55"},new[]{"#00D9E8","#2F6BFF"},new[]{"#9B3DFF","#FF4FB8"},new[]{"#2EE6A6","#2F6BFF"},new[]{"#FFD500","#FF5A00"},new[]{"#5A7BD8","#9B3DFF"},new[]{"#2EE65C","#00D9E8"},new[]{"#FF4F6E","#7A2CFF"}};
 string SceneArt(string name,int w,int h)
 {
  string low=name.ToLowerInvariant();string[] cols=null;
  foreach(var g in sceneGrad)foreach(var k in g[0].Split('|'))if(low.Contains(k)){cols=g.Skip(1).ToArray();break;}
  if(cols==null){int hsh=0;foreach(char ch in low)hsh=hsh*31+ch;cols=sceneGradFallback[(hsh&0x7fffffff)%sceneGradFallback.Length];}
  string key=string.Join("",cols).Replace("#","")+"-"+w+"x"+h;
  var sb=new System.Text.StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' width='"+w+"' height='"+h+"' viewBox='0 0 "+w+" "+h+"'><defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>");
  for(int i=0;i<cols.Length;i++)sb.Append("<stop offset='"+(cols.Length==1?0:(double)i/(cols.Length-1)).ToString("0.##",CultureInfo.InvariantCulture)+"' stop-color='"+cols[i]+"'/>");
  sb.Append("</linearGradient><linearGradient id='s' x1='0' y1='0' x2='0' y2='1'><stop offset='.45' stop-color='#000' stop-opacity='0'/><stop offset='1' stop-color='#000' stop-opacity='.38'/></linearGradient></defs>");
  sb.Append("<rect width='"+w+"' height='"+h+"' fill='url(#g)'/>"+(h>80?"<rect width='"+w+"' height='"+h+"' fill='url(#s)'/>":"")+"</svg>");
  return WallFile("scene-"+key,sb.ToString());
 }
 void SetCoverVisual(string id)
 {
  try{int pos=CoverPos(id);if(coverLabels.TryGetValue(id,out var l)&&l!=null){l.Text=CoverText(pos);l.TextColor=CoverTextColor(pos);}
   if(coverFills.TryGetValue(id,out var f)&&f!=null){int ch=120;f.Size2D=new Size2D(f.Size2D.Width,mCvH*(100-Math.Max(0,Math.Min(100,pos)))/100);}}catch{}
 }
 void BuildCoverPopup()
 {
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.05f,.07f,.12f,.55f)});
  int pw=940,ph=330,px=(1920-pw)/2,py=(1080-ph)/2;
  var panel=Panel(px+pw/2,py+ph/2,pw,ph,dark?new Color(.10f,.11f,.15f,.97f):new Color(.99f,.98f,.96f,.98f),44);panel.PivotPoint=new Position(.5f,.5f,.5f);panel.PositionUsesPivotPoint=true;panel.BorderlineWidth=1;panel.BorderlineColor=GlassEdge();panel.BoxShadow=new Shadow(80f,new Color(0,0,0,.35f),new Vector2(0,30));
  string name="";for(int i=0;i<haCoverId.Length;i++)if(haCoverId[i]==coverPopupEntity)name=haCoverName[i];
  var t1=Label("Volet "+name,0,34,pw,42,30,ink,"Semibold");t1.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(t1);
  var t2=Label("Choisir l’ouverture",0,80,pw,30,21,muted,"Medium");t2.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(t2);
  string[] lab={"Fermé","25 %","50 %","75 %","Ouvert"};
  int bw=150,bgp=22,total=5*bw+4*bgp,sx=(pw-total)/2,by=140,bh=110;
  for(int i=0;i<5;i++){
   int bx2=sx+i*(bw+bgp);bool sel=i==coverPopupSel;
   var b=new View{Position2D=new Position2D(bx2,by),Size2D=new Size2D(bw,bh),CornerRadius=new Vector4(18,18,18,18),BackgroundColor=sel?accent:Sf(.93f,.94f,.97f,1),BorderlineWidth=sel?3:0,BorderlineColor=accent};panel.Add(b);
   var bt=Label(lab[i],0,0,bw,bh,24,sel?white:ink,"Semibold");bt.HorizontalAlignment=HorizontalAlignment.Center;b.Add(bt);
  }
  var t3=Label("←/→ choisir · OK valider · Retour annuler",0,ph-52,pw,28,19,muted,"Medium");t3.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(t3);
 }
 // ---- colour lights (Govee neon, Govee H1630): on/off, brightness, Govee scenes, colours and whites, like the Govee app ----
 static readonly string[] lightColorNames={"Blanc chaud","Blanc neutre","Blanc froid","Rouge","Orange","Jaune","Vert","Cyan","Bleu","Violet","Rose"};
 static readonly string[] lightColorHex={"#FFC27A","#FFE7C4","#E4EEFF","#FF2D2D","#FF8A00","#FFD500","#2EE65C","#00D9E8","#2F6BFF","#9B3DFF","#FF4FB8"};
 static readonly int[] lightWhiteK={2700,4000,6500};
 static readonly int[] lightBriSteps={10,25,50,75,100};
 // Ambiances: one press sets colour (or white temperature in K when the hex is empty) and brightness. Works on Matter lights too.
 static readonly string[] moodNames={"Lecture","Détente","Cinéma","Soirée","Romantique","Coucher de soleil","Océan","Nuit"};
 static readonly string[] moodHex={"","","#FF7828","#783CFF","#FF285A","#FF6E14","#008CFF","#FF1E00"};
 static readonly int[] moodKelvin={4000,2700,0,0,0,0,0,0};
 static readonly int[] moodPct={80,40,12,35,30,55,45,5};
 static string SceneName(string e){switch(e){case "rainbow":return "Arc-en-ciel";case "snow_flake":return "Flocons";case "romantic":return "Romantique";case "movie":return "Cinéma";case "gradient":return "Dégradé";case "night_light":return "Veilleuse";case "white_light":return "Lumière blanche";case "work":return "Travail";case "reading":return "Lecture";default:var t=e.Replace('_',' ');return t.Length>0?char.ToUpper(t[0])+t.Substring(1):t;}}
 string LampName(string id){int i=Array.IndexOf(haLampId,id);return i>=0?haLampName[i]:id;}
 // Govee scenes come from Govee's public scene library (saved with the launcher); lights without one fall back to Home Assistant's effects.
 void LoadGovee()
 {
  try{using(var doc=JsonDocument.Parse(File.ReadAllText(Root+"govee-scenes.local.json")))foreach(var light in doc.RootElement.EnumerateObject()){
   var lib=new GoveeLib{Sku=JStr(light.Value,"sku"),Ip=JStr(light.Value,"ip")};
   foreach(var sc in light.Value.GetProperty("popular").EnumerateArray())lib.Popular.Add(new GoveeScene{Name=sc[0].GetString()??"",Code=sc[1].GetInt32(),Param=sc.GetArrayLength()>2?sc[2].GetString()??"":"",En=sc.GetArrayLength()>3?sc[3].GetString()??"":""});
   foreach(var c in light.Value.GetProperty("categories").EnumerateArray()){var list=new List<GoveeScene>();foreach(var sc in c.GetProperty("scenes").EnumerateArray())list.Add(new GoveeScene{Name=sc[0].GetString()??"",Code=sc[1].GetInt32(),Param=sc.GetArrayLength()>2?sc[2].GetString()??"":"",En=sc.GetArrayLength()>3?sc[3].GetString()??"":""});lib.Categories.Add(new KeyValuePair<string,List<GoveeScene>>(JStr(c,"name"),list));}
   goveeLibs[light.Name]=lib;
  }}catch{}
 }
 const int goveeQuick=7;
 string[] LightScenes(){
  if(goveeLibs.TryGetValue(lightId,out var lib)){var r=new List<string>();for(int i=0;i<Math.Min(goveeQuick,lib.Popular.Count);i++)r.Add(lib.Popular[i].Name);r.Add("Toutes les scènes…");return r.ToArray();}
  return lightEffects.TryGetValue(lightId,out var e)?e:new string[0];
 }
 void SendGovee(string id,GoveeScene sc)
 {
  if(sc.En==""){ShowError("Scène indisponible.");return;}
  string body="{\"p\":\"light/turn_on\",\"b\":{\"entity_id\":"+(cloudMembers.TryGetValue(id,out var cm)?EntJson(cm):"\""+id+"\"")+",\"effect\":\""+sc.En.Replace("\\","\\\\").Replace("\"","\\\"")+"\"}}";
  IntPtr ac;int r=app_control_create(out ac);
  if(r==0){try{
   r=app_control_set_app_id(ac,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(ac,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(ac,"quiet_ha_calls","["+body+"]");
   if(r==0)r=app_control_send_launch_request(ac,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(ac);}}
  if(r!=0){ShowError("Commande maison indisponible.");return;}
  HaCommanded(id);haLampState[id]="on";lightEffect[id]=sc.En;notice.Text=LampName(id)+" · scène « "+sc.Name+" »";SetLampVisual(id);
 }
 // French name of a Govee scene from the catalogue (Home Assistant reports the English one).
 string GoveeFrench(string id,string en)
 {
  if(goveeLibs.TryGetValue(id,out var lib))foreach(var c in lib.Categories)foreach(var sc in c.Value)if(sc.En==en)return sc.Name;
  return SceneName(en);
 }

 void ToggleLamp(string id){bool nowOn=!IsLampOn(id);foreach(var m in GroupMembers(id)){haLampState[m]=nowOn?"on":"off";int dot=m.IndexOf('.');SendHaAction(dot>0?m.Substring(0,dot):"light",nowOn?"turn_on":"turn_off",m);}SetLampVisual(id);}
 int NearestBriStep(string id){if(!IsLampOn(id)||!lightBri.TryGetValue(id,out var b)||b<0)return 0;int best=1;for(int i=0;i<lightBriSteps.Length;i++)if(Math.Abs(lightBriSteps[i]-b*100/255.0)<Math.Abs(lightBriSteps[best-1]-b*100/255.0))best=i+1;return best;}
 // Govee panel rows: 0 power/brightness, 1 scenes, 4 music mode, 5 options (DreamView, light zones), 3 moods, 2 colours.
 string LightObj(){return lightId.StartsWith("light.")?lightId.Substring(6):lightId;}
 string MusicSelect(){return MemberEntities(lightId,"select","_music_mode").First();}
 string MusicSwitch(){return MemberEntities(lightId,"switch","_music_mode").First();}
 string[] MusicModes(){return selectOptions.TryGetValue(MusicSelect(),out var o)?o.Take(10).ToArray():new string[0];}
 static readonly string[] optionSuffix={"_dreamview","_bottom_light","_side_light","_ripple_light"};
 static readonly string[] optionName={"DreamView","Lumière basse","Lumière latérale","Halo"};
 List<int> LightOptions(){var r=new List<int>();for(int i=0;i<optionSuffix.Length;i++)if(haLampState.ContainsKey("switch."+LightObj()+optionSuffix[i]))r.Add(i);return r;}
 static string MusicFrench(string m){switch(m){case "Rhythm":return "Rythme";case "Energic":return "Énergique";case "Spectrum":return "Spectre";case "Rolling":return "Roulement";case "Separation":return "Séparation";case "Hopping":return "Sautillant";case "Waves":return "Vagues";case "Sound Wave":return "Onde sonore";case "Shiny":return "Brillant";case "Colorful":return "Coloré";case "Day and Night":case "DayAndNight":return "Jour et nuit";case "Sprouting":return "Bourgeons";case "Stippling":return "Pointillé";case "Smudge":return "Estompé";case "Flowing Light":return "Lumière fluide";case "Luminous":return "Lumineux";case "PianoKeys":return "Piano";case "Fountain":return "Fontaine";case "DIY":return "Perso";default:return m;}}
 List<int> LightRows(){if(!goveeLamps.Contains(lightId))return new List<int>{0};var r=new List<int>{0};if(groupBulbs.ContainsKey(lightId))r.Add(6);if(LightScenes().Length>0)r.Add(1);if(MusicModes().Length>0)r.Add(4);if(LightOptions().Count>0)r.Add(5);r.Add(3);r.Add(2);return r;}
 int LightRowLength(int row){return row==6?(groupBulbs.TryGetValue(lightId,out var gb)?gb.Length:0):row==0?1+lightBriSteps.Length:row==1?LightScenes().Length:row==4?1+MusicModes().Length:row==5?LightOptions().Count:row==3?moodNames.Length:lightColorNames.Length;}
 // ---- light settings as a side sheet (BelAir OS 2 mockup): header with switch, brightness bar, then scenes, music, options, moods, colours ----
 int lightScroll;readonly Dictionary<int,List<int[]>> lightRects=new Dictionary<int,List<int[]>>();
 const int shW=820,shX=1920-40-820,shY=40,shH=1000,shTop=168,shPad=52;
 Color SheetFill(){return dark?new Color(.135f,.12f,.15f,.97f):prefWall==4?new Color(.99f,.97f,.94f,.98f):new Color(.985f,.985f,.99f,.98f);}
 Color ChipFill(){return dark?new Color(1,1,1,.12f):new Color(0,0,0,.055f);}
 void FocusRing(View v,bool f,float radius){v.BorderlineWidth=f?4:0;v.BorderlineColor=dark?white:accent;if(f){v.BoxShadow=new Shadow(30f,new Color(0,0,0,dark?.5f:.22f),new Vector2(0,12));}}
 static int ChipW(string t,int size){return (int)(t.Length*size*.56)+50;}
 void BuildLightPopup()
 {
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(0,0,0,dark?.5f:.32f)});
  var sheet=Panel(shX,shY,shW,shH,SheetFill(),44);sheet.BorderlineWidth=1;sheet.BorderlineColor=dark?new Color(1,1,1,.22f):new Color(0,0,0,.08f);sheet.BoxShadow=new Shadow(100f,new Color(0,0,0,dark?.6f:.25f),new Vector2(0,40));
  bool on=IsLampOn(lightId);int bri=lightBri.TryGetValue(lightId,out var b)?b:-1;string eff=lightEffect.TryGetValue(lightId,out var e)?e:"";
  bool music=haLampState.TryGetValue(MusicSwitch(),out var ms)&&ms=="on";string mmode=haSensor.TryGetValue(MusicSelect(),out var mm)?mm:"";
  sheet.Add(Label(LampName(lightId),shPad,46,shW-shPad*2-120,56,44,ink,"Bold"));
  sheet.Add(Label(!on?"Éteinte":music?"Mode musique · "+MusicFrench(mmode):(eff!=""&&eff!="none"&&eff!="None"?"Scène "+GoveeFrench(lightId,eff):"Allumée")+(bri>=0?" · "+(int)Math.Round(bri*100/255.0)+" %":""),shPad,106,shW-shPad*2-120,32,22,muted,"Medium"));
  lightRects.Clear();
  // header switch = row 0, col 0
  var sw=new View{Position2D=new Position2D(shW-shPad-92,62),Size2D=new Size2D(92,54),CornerRadius=new Vector4(27,27,27,27),BackgroundColor=on?new Color(.204f,.78f,.35f,1):dark?new Color(1,1,1,.22f):new Color(0,0,0,.14f)};sheet.Add(sw);
  sw.Add(new View{Position2D=new Position2D(on?43:5,5),Size2D=new Size2D(44,44),CornerRadius=new Vector4(22,22,22,22),BackgroundColor=white});
  FocusRing(sw,lightRow==0&&lightCol==0,27);
  var row0=new List<int[]>{new[]{shW-shPad-92,-110,92,54}};lightRects[0]=row0;
  var clip=new View{Position2D=new Position2D(0,shTop),Size2D=new Size2D(shW,shH-shTop-76),ClippingMode=ClippingModeType.ClipChildren};sheet.Add(clip);
  var body=new View{Size2D=new Size2D(shW,4000)};clip.Add(body);
  int y=0,inner=shW-shPad*2,focusTop=0,focusBottom=0,rowTop=0;
  void Track(int row,int x,int yy,int w,int h,bool f){if(!lightRects.ContainsKey(row))lightRects[row]=new List<int[]>();lightRects[row].Add(new[]{x,yy,w,h});if(f){focusTop=rowTop;focusBottom=yy+h;}}
  View Chip(int x,int yy,int w,int h,string text,bool act,bool f,int size=21){
   var c=new View{Position2D=new Position2D(x,yy),Size2D=new Size2D(w,h),CornerRadius=new Vector4(h/2,h/2,h/2,h/2),BackgroundColor=act?accent:ChipFill()};body.Add(c);
   var l=Label(text,0,0,w,h,size,act?white:ink,"Semibold");l.HorizontalAlignment=HorizontalAlignment.Center;c.Add(l);FocusRing(c,f,h/2);return c;}
  foreach(int row in LightRows()){
   rowTop=y;
   if(row==0){
    bool fb=lightRow==0&&lightCol>=1;int shown=fb?lightBriSteps[lightCol-1]:on&&bri>=0?(int)Math.Round(bri*100/255.0):0;
    body.Add(Label("Luminosité",shPad,y,400,30,22,muted,"Medium"));
    var vl=Label((fb?"OK pour ":"")+shown+" %",shPad,y,inner,30,22,fb?ink:muted,fb?"Semibold":"Medium");vl.HorizontalAlignment=HorizontalAlignment.End;body.Add(vl);
    var track=new View{Position2D=new Position2D(shPad,y+42),Size2D=new Size2D(inner,64),CornerRadius=new Vector4(32,32,32,32),BackgroundColor=dark?new Color(1,1,1,.14f):new Color(0,0,0,.07f),ClippingMode=ClippingModeType.ClipChildren};body.Add(track);
    track.Add(new View{Size2D=new Size2D(inner*Math.Max(0,Math.Min(100,shown))/100,64),BackgroundColor=dark?new Color(1,1,1,.88f):accent});
    for(int k=1;k<lightBriSteps.Length;k++)track.Add(new View{Position2D=new Position2D(inner*k/lightBriSteps.Length,20),Size2D=new Size2D(2,24),BackgroundColor=dark?new Color(1,1,1,.18f):new Color(0,0,0,.10f)});
    FocusRing(track,fb,32);
    int seg=inner/lightBriSteps.Length;for(int k=0;k<lightBriSteps.Length;k++)Track(0,shPad+k*seg,y+42,seg,64,fb&&lightCol==k+1);
    if(fb){focusTop=Math.Min(focusTop,y);}
    y+=42+64+40;continue;
   }
   string cap=row==6?"Spots":row==1?"Scènes":row==4?"Musique":row==5?"Options":row==3?"Ambiances":"Couleurs";
   body.Add(Label(cap,shPad,y,400,34,26,ink,"Semibold"));
   if(row==1&&goveeLibs.TryGetValue(lightId,out var glib0)){int total=glib0.Categories.Sum(c=>c.Value.Count);var r=Label(total+" · OK sur « Toutes » pour le catalogue",shPad,y+4,inner,30,20,muted,"Medium");r.HorizontalAlignment=HorizontalAlignment.End;body.Add(r);}
   y+=50;
   int n=LightRowLength(row);
   if(row==1){
    int gap=18,cw=(inner-3*gap)/4,ch=118;bool gv=goveeLibs.TryGetValue(lightId,out var glb);
    for(int i=0;i<n;i++){
     int x=shPad+(i%4)*(cw+gap),yy=y+(i/4)*(ch+gap);bool f=lightRow==1&&lightCol==i;
     string name=gv?LightScenes()[i]:SceneName(LightScenes()[i]);bool all=gv&&i>=Math.Min(goveeQuick,glb.Popular.Count);
     bool act=on&&!all&&(gv?eff==glb.Popular[i].En:eff==LightScenes()[i]);
     var t=new View{Position2D=new Position2D(x+cw/2,yy+ch/2),Size2D=new Size2D(cw,ch),PivotPoint=new Position(.5f,.5f,.5f),PositionUsesPivotPoint=true,CornerRadius=new Vector4(22,22,22,22),BackgroundColor=all?ChipFill():new Color(0,0,0,0),Scale=f?new Vector3(1.07f,1.07f,1):new Vector3(1,1,1)};body.Add(t);
     if(!all){t.Add(new ImageView{ResourceUrl=SceneArt(name,cw,ch),Size2D=new Size2D(cw,ch),CornerRadius=new Vector4(22,22,22,22)});
      var l=Label(name,14,ch-62,cw-24,52,21,white,"Semibold");l.MultiLine=true;l.LineWrapMode=LineWrapMode.Word;l.VerticalAlignment=VerticalAlignment.Bottom;t.Add(l);}
     else{var l=Label("Toutes →",0,0,cw,ch,21,ink,"Semibold");l.HorizontalAlignment=HorizontalAlignment.Center;t.Add(l);}
     FocusRing(t,f,22);if(!f&&act){t.BorderlineWidth=3;t.BorderlineColor=white;}
     Track(1,x,yy,cw,ch,f);
    }
    y+=((n+3)/4)*(ch+gap)-gap+40;continue;
   }
   if(row==2){
    int d=54,gap=Math.Max(8,(inner-n*d)/Math.Max(1,n-1));
    for(int i=0;i<n;i++){int x=shPad+i*(d+gap);bool f=lightRow==2&&lightCol==i;
     var c=new View{Position2D=new Position2D(x+d/2,y+d/2+4),Size2D=new Size2D(d,d),PivotPoint=new Position(.5f,.5f,.5f),PositionUsesPivotPoint=true,CornerRadius=new Vector4(d/2,d/2,d/2,d/2),BackgroundColor=HexColor(lightColorHex[i]),Scale=f?new Vector3(1.18f,1.18f,1):new Vector3(1,1,1)};body.Add(c);
     FocusRing(c,f,d/2);if(!f){c.BorderlineWidth=1;c.BorderlineColor=new Color(0,0,0,.12f);}
     Track(2,x,y+4,d,d,f);}
    if(lightRow==2){var nm=Label(lightColorNames[lightCol],shPad,y+70,inner,30,20,muted,"Medium");body.Add(nm);}
    y+=4+d+(lightRow==2?44:0)+40;continue;
   }
   // chip rows (spots, music, options, moods) flow onto several lines
   int cx=shPad,cy=y,chH=54,cgap=14;
   for(int i=0;i<n;i++){
    string text;bool act;Color dot=new Color(0,0,0,0);bool hasDot=false;
    if(row==6){string bulb=groupBulbs[lightId][i];act=haLampState.TryGetValue(bulb,out var bs)&&bs=="on";text="Spot "+(i+1)+(act?" · allumé":" · éteint");}
    else if(row==4){var modes=MusicModes();if(i==0){text=music?"Arrêter":"Activer";act=music;}else{text=MusicFrench(modes[i-1]);act=music&&modes[i-1]==mmode;}}
    else if(row==5){int oi=LightOptions()[i];text=optionName[oi];act=haLampState.TryGetValue("switch."+LightObj()+optionSuffix[oi],out var os)&&os=="on";}
    else{text=moodNames[i];act=false;hasDot=true;dot=HexColor(moodHex[i]!=""?moodHex[i]:moodKelvin[i]>=4000?"#FFE7C4":"#FFC27A");}
    int w=ChipW(text,21)+(hasDot?28:0);
    if(cx+w>shPad+inner){cx=shPad;cy+=chH+cgap;}
    bool f=lightRow==row&&lightCol==i;
    var c=Chip(cx,cy,w,chH,"",act,f);
    if(hasDot){c.Add(new View{Position2D=new Position2D(20,(chH-20)/2),Size2D=new Size2D(20,20),CornerRadius=new Vector4(10,10,10,10),BackgroundColor=dot});var l=Label(text,48,0,w-66,chH,21,act?white:ink,"Semibold");c.Add(l);}
    else{var l=Label(text,0,0,w,chH,21,act?white:ink,"Semibold");l.HorizontalAlignment=HorizontalAlignment.Center;c.Add(l);}
    Track(row,cx,cy,w,chH,f);cx+=w+cgap;
   }
   y=cy+chH+40;
  }
  int sheetH=Math.Min(shH,shTop+y+50),viewH=sheetH-shTop-76;sheet.Size2D=new Size2D(shW,sheetH);clip.Size2D=new Size2D(shW,viewH);
  if(lightRow==0&&lightCol==0)lightScroll=0;
  else{if(focusTop<lightScroll)lightScroll=Math.Max(0,focusTop);if(focusBottom+24>lightScroll+viewH)lightScroll=focusBottom+24-viewH;}
  lightScroll=Math.Max(0,Math.Min(lightScroll,Math.Max(0,y-viewH)));
  body.PositionY=-lightScroll;
  var hint=Label("↑/↓ ←/→ choisir · OK appliquer · Retour fermer",shPad,sheetH-60,inner,30,20,muted,"Medium");sheet.Add(hint);
 }
 // Up/Down move to the next line of tiles inside a section, then to the next section; Left/Right stay in the line.
 void MoveLight(string k)
 {
  var rows=LightRows();int ri=Math.Max(0,rows.IndexOf(lightRow));
  if(!lightRects.TryGetValue(lightRow,out var cur)||lightCol>=cur.Count){ri=Math.Max(0,Math.Min(rows.Count-1,ri+(k=="Up"?-1:1)));lightRow=rows[ri];lightCol=0;return;}
  var me=cur[lightCol];int cxm=me[0]+me[2]/2;
  if(lightRow==0&&lightCol==0&&k=="Down"){lightCol=Math.Max(1,NearestBriStep(lightId));return;}
  int best=-1;int bestLine=k=="Up"?int.MinValue:int.MaxValue,bestDx=int.MaxValue;
  for(int i=0;i<cur.Count;i++){var r=cur[i];bool ok=k=="Up"?r[1]<me[1]:r[1]>me[1];if(!ok)continue;
   int line=r[1],dx=Math.Abs(r[0]+r[2]/2-cxm);bool better=k=="Up"?line>bestLine||line==bestLine&&dx<bestDx:line<bestLine||line==bestLine&&dx<bestDx;
   if(better){best=i;bestLine=line;bestDx=dx;}}
  if(best>=0){lightCol=best;return;}
  int nri=ri+(k=="Up"?-1:1);if(nri<0||nri>=rows.Count)return;
  int nr=rows[nri];lightRow=nr;
  if(nr==0){lightCol=k=="Up"?Math.Max(1,NearestBriStep(lightId)):0;return;}
  if(!lightRects.TryGetValue(nr,out var nxt)||nxt.Count==0){lightCol=0;return;}
  int edge=k=="Up"?nxt.Max(r=>r[1]):nxt.Min(r=>r[1]);int pick=0,pd=int.MaxValue;
  for(int i=0;i<nxt.Count;i++)if(nxt[i][1]==edge){int dx=Math.Abs(nxt[i][0]+nxt[i][2]/2-cxm);if(dx<pd){pd=dx;pick=i;}}
  lightCol=pick;
 }
 void HandleLightKey(string k)
 {
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){lightOpen=false;BuildPage();return;}
  var rows=LightRows();int ri=Math.Max(0,rows.IndexOf(lightRow));
  if(k=="Up"||k=="Down"){MoveLight(k);BuildPage();return;}
  if(k=="Left"||k=="Right"){int len=LightRowLength(lightRow);lightCol=Math.Max(0,Math.Min(len-1,lightCol+(k=="Left"?-1:1)));BuildPage();return;}
  if(k!="Return"&&k!="KP_Enter")return;
  string id=lightId,body;
  if(lightRow==0&&lightCol==0){bool on=IsLampOn(id);haLampState[id]=on?"off":"on";body="{\"p\":\"light/"+(on?"turn_off":"turn_on")+"\",\"b\":{\"entity_id\":\""+id+"\"}}";}
  else if(lightRow==0){int pct=lightBriSteps[lightCol-1];haLampState[id]="on";lightBri[id]=(int)Math.Round(pct*2.55);body="{\"p\":\"light/turn_on\",\"b\":{\"entity_id\":\""+id+"\",\"brightness_pct\":"+pct+"}}";}
  else if(lightRow==1&&goveeLibs.TryGetValue(id,out var glib)){
   if(lightCol>=Math.Min(goveeQuick,glib.Popular.Count)){catalogOpen=true;catIndex=0;catSel=0;catPage=0;catOnTabs=true;BuildPage();return;}
   SendGovee(id,glib.Popular[lightCol]);BuildPage();return;
  }
  else if(lightRow==6){string b=groupBulbs[id][lightCol];bool bon=haLampState.TryGetValue(b,out var bs)&&bs=="on";haLampState[b]=bon?"off":"on";if(!bon)haLampState[id]="on";HaCommanded(b);
   body="{\"p\":\"light/"+(bon?"turn_off":"turn_on")+"\",\"b\":{\"entity_id\":\""+b+"\"}}";}
  else if(lightRow==4){
   string sw=MusicSwitch(),sel=MusicSelect();bool music=haLampState.TryGetValue(sw,out var ms)&&ms=="on";
   string sws=EntJson(MemberEntities(id,"switch","_music_mode")),sels=EntJson(MemberEntities(id,"select","_music_mode"));
   if(lightCol==0){haLampState[sw]=music?"off":"on";HaCommanded(sw);body="{\"p\":\"switch/"+(music?"turn_off":"turn_on")+"\",\"b\":{\"entity_id\":"+sws+"}}";}
   else{string opt=MusicModes()[lightCol-1];haSensor[sel]=opt;haLampState[sw]="on";haLampState[id]="on";HaCommanded(sw);HaCommanded(sel);
    body="{\"p\":\"select/select_option\",\"b\":{\"entity_id\":"+sels+",\"option\":\""+opt.Replace("\"","")+"\"},\"w\":0},{\"p\":\"switch/turn_on\",\"b\":{\"entity_id\":"+sws+"},\"w\":400}";}
  }
  else if(lightRow==5){int oi=LightOptions()[lightCol];string sw="switch."+LightObj()+optionSuffix[oi];bool o=haLampState.TryGetValue(sw,out var os)&&os=="on";haLampState[sw]=o?"off":"on";HaCommanded(sw);
   body="{\"p\":\"switch/"+(o?"turn_off":"turn_on")+"\",\"b\":{\"entity_id\":\""+sw+"\"}}";}
  else if(lightRow==1){string fx=LightScenes()[lightCol];haLampState[id]="on";lightEffect[id]=fx;body="{\"p\":\"light/turn_on\",\"b\":{\"entity_id\":\""+id+"\",\"effect\":\""+fx+"\"}}";}
  else if(lightRow==3){int m=lightCol;haLampState[id]="on";lightEffect[id]="";lightBri[id]=(int)Math.Round(moodPct[m]*2.55);
   string colour;if(moodHex[m]==""){colour="\"color_temp_kelvin\":"+moodKelvin[m];}else{var c=HexColor(moodHex[m]);colour="\"rgb_color\":["+(int)(c.R*255)+","+(int)(c.G*255)+","+(int)(c.B*255)+"]";}
   body="{\"p\":\"light/turn_on\",\"b\":{\"entity_id\":\""+id+"\","+colour+",\"brightness_pct\":"+moodPct[m]+"}}";}
  else if(lightCol<3){haLampState[id]="on";lightEffect[id]="";body="{\"p\":\"light/turn_on\",\"b\":{\"entity_id\":\""+id+"\",\"color_temp_kelvin\":"+lightWhiteK[lightCol]+"}}";}
  else{var c=HexColor(lightColorHex[lightCol]);haLampState[id]="on";lightEffect[id]="";body="{\"p\":\"light/turn_on\",\"b\":{\"entity_id\":\""+id+"\",\"rgb_color\":["+(int)(c.R*255)+","+(int)(c.G*255)+","+(int)(c.B*255)+"]}}";}
  IntPtr ac;int r=app_control_create(out ac);
  if(r==0){try{
   r=app_control_set_app_id(ac,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(ac,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(ac,"quiet_ha_calls","["+body+"]");
   if(r==0)r=app_control_send_launch_request(ac,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(ac);}}
  HaCommanded(id);if(r!=0)ShowError("Commande maison indisponible.");
  BuildPage();SetLampVisual(id);
 }
 // ---- full Govee scene catalogue: category tabs, then a paged grid of scenes ----
 void BuildSceneCatalog()
 {
  if(!goveeLibs.TryGetValue(lightId,out var lib)||lib.Categories.Count==0){catalogOpen=false;return;}
  catIndex=Math.Max(0,Math.Min(catIndex,lib.Categories.Count-1));var scenes=lib.Categories[catIndex].Value;int perPage=catCols*catRowsPerPage,pages=(scenes.Count+perPage-1)/perPage;
  catPage=Math.Max(0,Math.Min(catPage,pages-1));
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(0,0,0,dark?.55f:.35f)});
  int pw=1840,ph=1000,px=(1920-pw)/2,py=(1080-ph)/2;
  var panel=Panel(px,py,pw,ph,SheetFill(),44);panel.BorderlineWidth=1;panel.BorderlineColor=dark?new Color(1,1,1,.22f):new Color(0,0,0,.08f);panel.BoxShadow=new Shadow(100f,new Color(0,0,0,dark?.6f:.25f),new Vector2(0,40));
  panel.Add(Label("Scènes · "+LampName(lightId),52,40,1200,56,44,ink,"Bold"));
  panel.Add(Label(scenes.Count+" scènes dans « "+lib.Categories[catIndex].Key+" »"+(pages>1?" · page "+(catPage+1)+"/"+pages:""),52,100,1200,32,22,muted,"Medium"));
  int n=lib.Categories.Count,gap=12,tw=Math.Min(220,(pw-96-(n-1)*gap)/n);
  for(int i=0;i<n;i++){
   bool on=i==catIndex,f=catOnTabs&&on;
   var tab=new View{Position2D=new Position2D(52+i*(tw+gap),150),Size2D=new Size2D(tw,54),CornerRadius=new Vector4(27,27,27,27),BackgroundColor=on?(dark?white:accent):ChipFill()};panel.Add(tab);FocusRing(tab,f,27);if(f&&dark){tab.BorderlineColor=accent;}
   var t=Label(lib.Categories[i].Key,6,0,tw-12,54,21,on?(dark?new Color(.05f,.05f,.07f,1):white):ink,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;tab.Add(t);
  }
  gap=18;int cw=(pw-104-(catCols-1)*gap)/catCols,ch=128;
  for(int k=0;k<perPage;k++){
   int idx=catPage*perPage+k;if(idx>=scenes.Count)break;
   bool f=!catOnTabs&&catSel==idx,cur=lightEffect.TryGetValue(lightId,out var ce)&&ce==scenes[idx].En;
   int x=52+(k%catCols)*(cw+gap),y=232+(k/catCols)*(ch+gap);
   var chip=new View{Position2D=new Position2D(x+cw/2,y+ch/2),Size2D=new Size2D(cw,ch),PivotPoint=new Position(.5f,.5f,.5f),PositionUsesPivotPoint=true,CornerRadius=new Vector4(22,22,22,22),Scale=f?new Vector3(1.06f,1.06f,1):new Vector3(1,1,1)};panel.Add(chip);
   chip.Add(new ImageView{ResourceUrl=SceneArt(scenes[idx].Name,cw,ch),Size2D=new Size2D(cw,ch),CornerRadius=new Vector4(22,22,22,22)});
   var l=Label(scenes[idx].Name,16,ch-66,cw-30,54,21,white,"Semibold");l.MultiLine=true;l.LineWrapMode=LineWrapMode.Word;l.VerticalAlignment=VerticalAlignment.Bottom;chip.Add(l);
   FocusRing(chip,f,22);if(!f&&cur){chip.BorderlineWidth=3;chip.BorderlineColor=white;}
  }
  var hint=Label("←/→ "+(catOnTabs?"catégorie":"choisir")+" · ↓/↑ parcourir les pages · OK lancer la scène · Retour fermer",0,ph-60,pw,30,20,muted,"Medium");hint.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(hint);
 }
 void HandleCatalogKey(string k)
 {
  if(!goveeLibs.TryGetValue(lightId,out var lib)){catalogOpen=false;BuildPage();return;}
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){catalogOpen=false;BuildPage();return;}
  var scenes=lib.Categories[catIndex].Value;int perPage=catCols*catRowsPerPage;
  if(catOnTabs){
   if(k=="Left"||k=="Right"){catIndex=(catIndex+(k=="Left"?-1:1)+lib.Categories.Count)%lib.Categories.Count;catSel=0;catPage=0;BuildPage();return;}
   if(k=="Down"||k=="Return"||k=="KP_Enter"){if(scenes.Count>0){catOnTabs=false;catSel=catPage*perPage;BuildPage();}return;}
   return;
  }
  if(k=="Left"||k=="Right"){int col=catSel%catCols,rowStart=catSel-col;int next=catSel+(k=="Left"?-1:1);if(next>=rowStart&&next<Math.Min(rowStart+catCols,scenes.Count))catSel=next;BuildPage();return;}
  if(k=="Up"){if(catSel-catCols<0){catOnTabs=true;}else{catSel-=catCols;}catPage=catSel/perPage;BuildPage();return;}
  if(k=="Down"){if(catSel+catCols<scenes.Count)catSel+=catCols;else if(catSel/catCols<(scenes.Count-1)/catCols)catSel=scenes.Count-1;catPage=catSel/perPage;BuildPage();return;}
  if(k=="Return"||k=="KP_Enter"){SendGovee(lightId,scenes[catSel]);BuildPage();}
 }
 void HandleCoverPopupKey(string k)
 {
  int[] pr={0,25,50,75,100};
  if(k=="Left"||k=="Right"){coverPopupSel=Math.Max(0,Math.Min(4,coverPopupSel+(k=="Left"?-1:1)));BuildPage();return;}
  if(k=="Return"||k=="KP_Enter"){int pos=pr[coverPopupSel];haCoverPos[coverPopupEntity]=pos;coverPopupOpen=false;SendHaCoverPos(coverPopupEntity,pos);BuildPage();SetCoverVisual(coverPopupEntity);notice.Text="Volet réglé";return;}
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){coverPopupOpen=false;BuildPage();return;}
 }
 void ReceiveHaData(string json)
 {
  try{using(var doc=JsonDocument.Parse(json))foreach(var el in doc.RootElement.EnumerateArray()){
   string id=el.GetProperty("entity").GetString();if(id==null)continue;
   string st=el.TryGetProperty("state",out var sv)?sv.GetString():"";
   if(id.StartsWith("sensor.")||id.StartsWith("vacuum.")||id.StartsWith("automation.")){if(!HaRecent(id,4))haSensor[id]=st??"";continue;}
   if(id.StartsWith("select.")){if(HaRecent(id,4))continue;haSensor[id]=st??"";if(el.TryGetProperty("options",out var ov)&&ov.ValueKind==JsonValueKind.Array){var ol=new List<string>();foreach(var o in ov.EnumerateArray())ol.Add(o.GetString()??"");selectOptions[id]=ol.ToArray();}continue;}
   if(id.StartsWith("cover.")){if(HaRecent(id,45))continue;haCoverState[id]=st??"";if(el.TryGetProperty("pos",out var pv)&&pv.ValueKind==JsonValueKind.Number){int p=pv.GetInt32();if(p>=0)haCoverPos[id]=p;}}
   else if(!HaRecent(id,4)){
    haLampState[id]=st??"";
    if(el.TryGetProperty("color",out var cl)&&cl.ValueKind==JsonValueKind.True){
     if(goveeLamps.Contains(id))colorLights.Add(id); // only the Govee lamps get the scene panel; other colour lamps (IKEA) stay plain on/off
     if(el.TryGetProperty("bri",out var bv)&&bv.ValueKind==JsonValueKind.Number)lightBri[id]=bv.GetInt32();
     lightRgb[id]=el.TryGetProperty("rgb",out var rv)&&rv.ValueKind==JsonValueKind.String?rv.GetString()??"":"";
     lightEffect[id]=el.TryGetProperty("effect",out var ev)&&ev.ValueKind==JsonValueKind.String?ev.GetString()??"":"";
     if(el.TryGetProperty("effects",out var es)&&es.ValueKind==JsonValueKind.Array){var list=new List<string>();foreach(var x in es.EnumerateArray())list.Add(x.GetString()??"");lightEffects[id]=list.ToArray();}
    }
   }
  }}catch{}
  if(lightOpen&&(page==5||page==0)&&!HaRecent(lightId,4))BuildPage();
  if(backlightHint!=null)backlightHint.Text=LampStateText(backlightId);
  if(page==5&&!coverPopupOpen){foreach(var e in haCoverId)SetCoverVisual(e);foreach(var e in haLampId)SetLampVisual(e);}
  UpdateHomeSensors();UpdateDevices();
  WriteState("ha_data");
 }
 static void HTlog(string m){try{var f="/tmp/quiet-home-timing.txt";if(File.Exists(f)&&new FileInfo(f).Length>40000)File.Delete(f);File.AppendAllText(f,DateTime.Now.ToString("HH:mm:ss.fff")+" "+m+"\n");}catch{}}
 void SendHaAction(string domain,string service,string entity)
 {
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_domain",domain);
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_service",service);
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_entity",entity);
   var sw=System.Diagnostics.Stopwatch.StartNew();
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
   HTlog("send "+service+" "+entity+" "+sw.ElapsedMilliseconds+"ms r="+r);
  }finally{app_control_destroy(c);}}
  HaCommanded(entity);
  if(r!=0)ShowError("Commande maison indisponible.");
 }
 void SendHaBatch(string json)
 {
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_batch",json);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
  HaCommanded(cuisineGroup);
  if(r!=0)ShowError("Commande maison indisponible.");
 }
 // One launch per command: note what we just commanded so a poll does not race it or overwrite the optimistic state,
 // then re-read Home Assistant once the lamp has switched (8 s) and once a shutter has finished moving (48 s).
 void HaCommanded(string id)
 {
  lastHaRequest=DateTime.Now;if(id!=null)haCmdAt[id]=DateTime.Now;haRefreshTicks=0;
  if(haRefreshTimer!=null){haRefreshTimer.Stop();haRefreshTimer.Dispose();}
  haRefreshTimer=new Tizen.NUI.Timer(8000);
  haRefreshTimer.Tick+=(o,a)=>{haRefreshTicks++;if((haRefreshTicks==1||haRefreshTicks==6)&&page==5&&!coverPopupOpen){lastHaRequest=DateTime.Now;RequestHaStates();}return haRefreshTicks<6;};
  haRefreshTimer.Start();
 }
 bool HaRecent(string id,int seconds){return haCmdAt.TryGetValue(id,out var t)&&(DateTime.Now-t).TotalSeconds<seconds;}
 void RequestHaStates()
 {
  if(isPaused)return;
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_states_request","1");
   if(r==0)r=app_control_add_extra_data(c,"quiet_ha_sensors",haSensorWanted);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
 }
 void Toolbar(string label,string action,string icon,int n)
 {
  int x=1652+n*64,y=46,sz=60;
  var card=Panel(x+sz/2,y+sz/2,sz,sz,GlassFill(),sz/2);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;LiquidGlass(card);
  if(icon=="hdmi")card.Add(new ImageView{ResourceUrl=IconFile("tv",HexOf(ink)),Position2D=new Position2D(14,14),Size2D=new Size2D(32,32)});
  else card.Add(new ImageView{ResourceUrl=Root+"symbol-"+icon+".png",Color=ink,Position2D=new Position2D(16,16),Size2D=new Size2D(28,28)});
  slots.Add(new Slot{Card=card,Name=label,Action=action,Description=label,Row=-1,X=x,Y=y,W=sz,H=sz});
 }
 // Paris (the TV's local time) and Buenos Aires (UTC-3 all year, Argentina has no daylight saving time).
 void WorldClock()
 {
  int x=1400,y=150,w=440,h=60;
  var card=Panel(x,y,w,h,GlassFill(),30);card.BorderlineWidth=1;card.BorderlineColor=GlassEdge();
  card.Add(Label("PARIS",28,0,90,h,15,muted,"Semibold"));
  worldParis=Label("",96,0,110,h,26,ink,"Semibold");card.Add(worldParis);
  card.Add(new View{Position2D=new Position2D(w/2-1,16),Size2D=new Size2D(2,h-32),BackgroundColor=Sf(.85f,.86f,.9f,1)});
  card.Add(Label("BUENOS AIRES",w/2+20,0,124,h,15,muted,"Semibold"));
  worldBa=Label("",w/2+136,0,76,h,26,ink,"Semibold");card.Add(worldBa);
  UpdateWorldClock();
 }
 void UpdateWorldClock()
 {
  if(worldParis==null)return;
  worldParis.Text=DateTime.Now.ToString("HH:mm");
  worldBa.Text=DateTime.UtcNow.AddHours(-3).ToString("HH:mm");
 }
 void NewsDropdownButton()
 {
  int x=96,y=150,w=410,h=60;
  var card=Panel(x+w/2,y+h/2,w,h,GlassFill(),30);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
  newsTopTitle=Label(NewsButtonText(),18,0,w-36,h,20,ink,"Semibold");newsTopTitle.HorizontalAlignment=HorizontalAlignment.Center;card.Add(newsTopTitle);
  newsButtonSlotIndex=slots.Count;
  slots.Add(new Slot{Card=card,Label=newsTopTitle,Name="Actualités",Action="news",Description="Paris · France · Monde · Bourse",Row=-1,X=x,Y=y,W=w,H=h});
 }
 string NewsButtonText()
 {
  int total=0;foreach(var section in newsSections)total+=section.Items.Count;
  return total==0?"ACTUALITÉS   ⌄":"ACTUALITÉS   ⌄   "+total;
 }
 List<NewsArticle> NewsItems(int index)
 {
  if(index<0||index>=newsSections.Count)return new List<NewsArticle>();
  return newsSections[index].Items??new List<NewsArticle>();
 }
 string NewsUpdatedText()
 {
  DateTime updated;
  return DateTime.TryParse(newsUpdated,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out updated)?"Mis à jour à "+updated.ToLocalTime().ToString("HH:mm"):"En attente du flux d’actualités";
 }
 void BuildNewsDropdown()
 {
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.05f,.07f,.12f,.52f)});
  int x=498,y=150,w=924,h=747;
  var panel=Panel(x,y,w,h,Sf(.99f,.99f,1,1),28);
  panel.Add(Label("À la une",28,18,440,44,31,ink,"Display"));
  panel.Add(Label(NewsUpdatedText()+" · 5 titres par rubrique",28,63,868,28,17,muted,"Medium"));
  newsCategorySlotStart=slots.Count;
  int categoryWidth=207,categoryGap=14;
  for(int i=0;i<newsSectionNames.Length;i++){
   int categoryX=24+i*(categoryWidth+categoryGap);
   var category=new View{Position2D=new Position2D(categoryX,102),Size2D=new Size2D(categoryWidth,53),BackgroundColor=i==newsCategoryIndex?new Color(.15f,.24f,.48f,1):Sf(.93f,.94f,.97f,1),CornerRadius=new Vector4(16,16,16,16)};
   category.PivotPoint=new Position(.5f,.5f,.5f);category.PositionUsesPivotPoint=true;
   Add(category);
   var label=Label(newsSectionNames[i].ToUpperInvariant(),10,0,categoryWidth-20,53,18,i==newsCategoryIndex?white:ink,"Semibold");label.HorizontalAlignment=HorizontalAlignment.Center;category.Add(label);
   slots.Add(new Slot{Card=category,Label=label,Name=newsSectionNames[i],Action="news_category:"+i,Description=newsSectionNames[i],Row=-4,X=x+categoryX,Y=y+102,W=categoryWidth,H=53});
  }
  quoteSlotStart=-1;
  var articles=NewsItems(newsCategoryIndex);
  int rowsTop=174;
  if(newsCategoryIndex==3&&marketQuotes.Count>0){
   int n=QuoteCount(),perRow=QuotePerRow(),gap=12,cw=(880-(perRow-1)*gap)/perRow,rows=(n+perRow-1)/perRow;
   rowsTop=164+rows*72+10;
   for(int i=0;i<n;i++){
    var q=marketQuotes[i];double p=ChangePct(q);int col=i%perRow,row=i/perRow;
    int chipX=x+22+col*(cw+gap),chipY=y+164+row*72;
    var chip=new View{Size2D=new Size2D(cw,62),BackgroundColor=Sf(.92f,.93f,.96f,1),CornerRadius=new Vector4(16,16,16,16),PivotPoint=new Position(.5f,.5f,.5f),PositionUsesPivotPoint=true,Position=new Position(chipX+cw/2f,chipY+31,0)};Add(chip);
    if(i==0)quoteSlotStart=slots.Count;
    slots.Add(new Slot{Card=chip,Name=q.Name,Action="quote:"+i,Description="OK pour le graphique du jour",Row=-6-row,X=chipX,Y=chipY,W=cw,H=62});
    chip.Add(Label(q.Name,14,4,cw-28,22,14,muted,"Semibold"));
    chip.Add(Label(PriceText(q,q.Price),14,28,cw-100,30,18,ink,"Semibold"));
    var pct=Label((p>=0?"+":"−")+Math.Abs(p).ToString("0.00",frCulture)+" %",cw-92,28,80,30,15,PctColor(p,dark),"Semibold");pct.HorizontalAlignment=HorizontalAlignment.End;chip.Add(pct);
   }
  }
  newsArticleSlotStart=slots.Count;
  if(articles.Count==0){
   panel.Add(Label("Les articles apparaîtront dès que le flux sera disponible.",30,rowsTop+23,850,96,23,muted,"Medium"));
  }else{
   int count=Math.Min(ArticleLimit(),articles.Count);
   for(int i=0;i<count;i++){
    var article=articles[i];int articleY=rowsTop+i*108;
    var row=new View{Position2D=new Position2D(22,articleY),Size2D=new Size2D(880,96),BackgroundColor=Sf(.95f,.96f,.98f,1),CornerRadius=new Vector4(16,16,16,16)};
    row.PivotPoint=new Position(.5f,.5f,.5f);row.PositionUsesPivotPoint=true;
    Add(row);
    int tx=18;
    if(article.Image!=""){row.Add(new ImageView{ResourceUrl=article.Image,Position2D=new Position2D(6,6),Size2D=new Size2D(150,84),FittingMode=FittingModeType.ScaleToFill,DesiredWidth=300,DesiredHeight=168,CornerRadius=new Vector4(12,12,12,12)});tx=174;}
    var title=Label(article.Title,tx,7,862-tx,52,19,ink,"Semibold");title.MultiLine=true;title.LineWrapMode=LineWrapMode.Word;row.Add(title);
    string byline=article.Source+(article.Published==""?"":" · "+article.Published);
    row.Add(Label(byline,tx,60,862-tx,25,15,muted,"Medium"));
    slots.Add(new Slot{Card=row,Label=title,Name=article.Title,Action="news_item:"+i,Description=byline,Row=-5,X=x+22,Y=y+articleY,W=880,H=96});
   }
  }
  panel.Add(Label("←/→ rubrique · ↓ articles · OK détail",28,720,850,25,16,muted,"Medium"));
 }
 NewsArticle SelectedNewsArticle()
 {
  var articles=NewsItems(newsCategoryIndex);
  return newsArticleIndex>=0&&newsArticleIndex<articles.Count?articles[newsArticleIndex]:null;
 }
 // ---- intraday chart for one quote (5-minute points of the current or last session) ----
 static readonly HashSet<string> usdQuotes=new HashSet<string>{"QQQ","NVDA","AMD","MU","SNDK","GOOGL","AAPL","GC=F","HG=F"};
 static string PriceText(MarketQuote q,double v){string t=Grp(v.ToString(q.Price<20?"N4":"N2",frCulture));return usdQuotes.Contains(q.Symbol)?t+" $":t;}
 int QuoteCount(){return Math.Min(16,marketQuotes.Count);}
 int QuotePerRow(){int n=QuoteCount();return n<=4?4:n<=6?3:4;}
 void RequestSeries(string symbol,int range)
 {
  if(isPaused)return;string key=symbol+"|"+rangeIds[range];seriesAt[key+"#asked"]=DateTime.Now;
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_series_request",key);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
 }
 void ReceiveSeries(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){
   var el=doc.RootElement;var q=new MarketQuote{Symbol=JStr(el,"Symbol"),Price=JNum(el,"Price"),Previous=JNum(el,"Previous")};
   foreach(var v in el.GetProperty("Points").EnumerateArray())q.Points.Add(v.GetDouble());
   foreach(var v in el.GetProperty("Times").EnumerateArray())q.Times.Add(v.GetInt64());
   string key=q.Symbol+"|"+JStr(el,"Range");seriesCache[key]=q;seriesAt[key]=DateTime.Now;
   if(quoteDetailOpen&&page==0&&newsMenuOpen&&quoteIndex<marketQuotes.Count&&marketQuotes[quoteIndex].Symbol+"|"+rangeIds[quoteRange]==key)BuildPage();
  }}catch{}
 }
 // The quote to chart for the selected scale: today's quote is live; longer scales come from the history cache (refetched after 15 min).
 MarketQuote ChartQuote(MarketQuote live,out bool loading)
 {
  loading=false;if(quoteRange==0)return live;
  string key=live.Symbol+"|"+rangeIds[quoteRange];
  bool fresh=seriesAt.TryGetValue(key,out var at)&&DateTime.Now-at<TimeSpan.FromMinutes(15);
  bool asked=seriesAt.TryGetValue(key+"#asked",out var askedAt)&&DateTime.Now-askedAt<TimeSpan.FromSeconds(20);
  if(!fresh&&!asked)RequestSeries(live.Symbol,quoteRange);
  if(seriesCache.TryGetValue(key,out var q)&&q.Points.Count>1)return q;
  loading=true;return null;
 }
 string AxisTime(long unix)
 {
  var t=DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime();
  switch(quoteRange){
   case 0:return t.ToString("HH:mm");
   case 1:return frDays[(int)t.DayOfWeek].Substring(0,3)+". "+t.Day+" · "+t.ToString("HH")+" h";
   case 2:return t.Day+" "+frMonthShort[t.Month-1];
   case 5:return t.Year.ToString(CultureInfo.InvariantCulture);
   default:return frMonthShort[t.Month-1]+" "+t.ToString("yy");
  }
 }
 void BuildQuoteDetail()
 {
  if(marketQuotes.Count==0)return;quoteIndex=Math.Max(0,Math.Min(quoteIndex,QuoteCount()-1));
  var live=marketQuotes[quoteIndex];bool loading;var q=ChartQuote(live,out loading);
  double reference=q!=null?q.Previous:live.Previous,p=reference>0?(live.Price-reference)/reference*100:0;
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.03f,.05f,.09f,.72f)});
  int px=170,py=140,pw=1580,ph=820;
  var panel=Panel(px,py,pw,ph,Sf(.99f,.99f,1,1),30);
  panel.Add(Label(live.Name,48,30,900,48,38,ink,"Semibold"));
  panel.Add(Label((live.Symbol.StartsWith("^")?"Indice":live.Symbol=="EURUSD=X"?"Devise":live.Symbol.EndsWith("=F")?"Matière première · contrat à terme":live.Symbol=="QQQ"?"ETF":"Action")+" · "+QuoteTime(live)+" · "+(quoteIndex+1)+"/"+QuoteCount(),48,80,900,30,19,muted,"Medium"));
  panel.Add(Label(PriceText(live,live.Price),44,118,600,84,68,ink,"Display"));
  double diff=live.Price-reference;
  panel.Add(Label(loading?"Chargement de l’historique…":FmtPct(p)+"   "+(diff>=0?"+":"−")+PriceText(live,Math.Abs(diff))+"   "+rangePeriod[quoteRange],48,204,700,40,26,loading?muted:PctColor(p,dark),"Semibold"));
  double hi=live.Price,lo=live.Price;if(q!=null)foreach(var v in q.Points){if(v>hi)hi=v;if(v<lo)lo=v;}
  string[] caps=quoteRange==0?new[]{"OUVERTURE","PLUS HAUT","PLUS BAS","CLÔTURE VEILLE"}:new[]{"DÉBUT DE PÉRIODE","PLUS HAUT","PLUS BAS","RÉFÉRENCE"};
  double[] vals={q!=null&&q.Points.Count>0?q.Points[0]:0,q!=null?hi:0,q!=null?lo:0,reference};
  for(int i=0;i<4;i++){int x=760+i*196;panel.Add(Label(caps[i],x,122,190,22,15,muted,"Semibold"));panel.Add(Label(vals[i]>0?PriceText(live,vals[i]):"—",x,146,190,36,25,ink,"Semibold"));}
  for(int i=0;i<rangeIds.Length;i++){
   bool on=i==quoteRange;int x=760+i*128;
   var chip=new View{Position2D=new Position2D(x,206),Size2D=new Size2D(116,44),BackgroundColor=on?accent:Sf(.88f,.90f,.94f,1),CornerRadius=new Vector4(22,22,22,22)};panel.Add(chip);
   var t=Label(rangeNames[i],0,0,116,44,18,on?white:ink,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;chip.Add(t);
  }
  int cx=48,cy=272,cw=pw-96-110,ch=430;
  var frame=new View{Position2D=new Position2D(cx,cy),Size2D=new Size2D(cw,ch),BackgroundColor=Sf(.96f,.97f,.99f,1),CornerRadius=new Vector4(20,20,20,20)};panel.Add(frame);
  var host=new View{Position2D=new Position2D(12,14),Size2D=new Size2D(cw-24,ch-28)};frame.Add(host);
  string stroke=p>=0?(dark?"#8CF1BF":"#1A8C4D"):(dark?"#FF8A80":"#C7291F");
  if(q!=null)SetChart(host,q,stroke,dark?"#C9D3E0":"#5B6575");
  if(loading||q==null||q.Points.Count<2){var m=Label(loading?"Chargement…":"Pas encore de cotation sur cette période.",0,0,cw,ch,24,muted,"Medium");m.HorizontalAlignment=HorizontalAlignment.Center;frame.Add(m);}
  else{
   // The chart's vertical range includes the reference (dashed line), so the axis labels must too.
   double axisTop=q.Previous>0?Math.Max(hi,q.Previous):hi,axisBottom=q.Previous>0?Math.Min(lo,q.Previous):lo;
   panel.Add(Label(PriceText(live,axisTop),cx+cw+10,cy+8,104,26,16,muted,"Medium"));
   panel.Add(Label(PriceText(live,axisBottom),cx+cw+10,cy+ch-34,104,26,16,muted,"Medium"));
   if(q.Times.Count>=2)for(int i=0;i<5;i++){
    int idx=(int)Math.Round((q.Times.Count-1)*i/4.0);
    var l=Label(AxisTime(q.Times[idx]),cx+(int)((cw-150)*i/4.0),cy+ch+10,150,26,16,muted,"Medium");l.HorizontalAlignment=i==0?HorizontalAlignment.Begin:i==4?HorizontalAlignment.End:HorizontalAlignment.Center;panel.Add(l);
   }
  }
  panel.Add(Label("Pointillés : "+(quoteRange==0?"clôture de la veille":"niveau au début de la période")+" · un point par "+rangeStep[quoteRange]+" · données Yahoo Finance, parfois différées",48,ph-62,pw-96,26,17,muted,"Medium"));
 }
 int ArticleLimit(){if(newsCategoryIndex!=3||marketQuotes.Count==0)return 5;int rows=(QuoteCount()+QuotePerRow()-1)/QuotePerRow();return rows<=1?4:rows==2?3:2;}
 // ---- full article: fetched on demand by the helper, cached for the session ----
 void RequestArticle(NewsArticle article)
 {
  if(article==null||article.Url==""||articleCache.ContainsKey(article.Url)||isPaused)return;
  if(articlePending==article.Url&&(DateTime.Now-articlePendingAt).TotalSeconds<15)return;
  articlePending=article.Url;articlePendingAt=DateTime.Now;
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_article_request",article.Url);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
  WriteState("article_requested:"+r);
 }
 void ReceiveArticle(string json)
 {
  string url="";
  try{using(var doc=JsonDocument.Parse(json)){
   var root=doc.RootElement;url=root.GetProperty("Url").GetString()??"";
   var full=new ArticleFull{Image=root.TryGetProperty("Image",out var image)?image.GetString()??"":""};
   foreach(var b in root.GetProperty("Blocks").EnumerateArray())full.Blocks.Add(b.GetString()??"");
   if(articleCache.Count>40)articleCache.Clear();
   articleCache[url]=full;
  }}catch{}
  if(articlePending==url)articlePending="";
  var current=SelectedNewsArticle();
  if(page==0&&newsMenuOpen&&newsDetailOpen&&current!=null&&current.Url==url){BuildPage();area=9;UpdateFocus(false);}
  WriteState("article_received");
 }
 static string Esc(string t){return t.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");}
 // Splits the article into screens using a line budget (≈104 characters per line at this size), never cutting a word.
 List<string> ArticlePages(NewsArticle article)
 {
  var pages=new List<string>();if(article==null){pages.Add("");return pages;}
  ArticleFull full;articleCache.TryGetValue(article.Url,out full);
  var blocks=full!=null&&full.Blocks.Count>0?full.Blocks:new List<string>{article.Summary==""?"Texte indisponible.":article.Summary};
  const int perLine=104;var current=new System.Text.StringBuilder();int used=0;
  int Capacity()=>pages.Count==0?10:21;
  void Flush(){pages.Add(current.ToString());current.Clear();used=0;}
  foreach(var raw in blocks){
   bool head=raw.StartsWith("#");string text=head?raw.Substring(1):raw;
   while(text.Length>0){
    int gap=used>0?1:0,room=Capacity()-used-gap,need=(text.Length+perLine-1)/perLine;
    if(room<=0||(head&&room<2)){Flush();continue;}
    string piece=text;
    if(need>room){
     int limit=Math.Min(text.Length-1,room*perLine);int cut=text.LastIndexOf(' ',limit);if(cut<limit/2)cut=limit;
     piece=text.Substring(0,cut);
    }
    if(gap>0)current.Append("\n\n");
    current.Append(head?"<font family='Quiet Inter Semibold'>"+Esc(piece)+"</font>":Esc(piece));
    used+=gap+(piece.Length+perLine-1)/perLine;text=text.Substring(piece.Length).TrimStart();
    if(text.Length>0)Flush();
   }
  }
  if(current.Length>0||pages.Count==0)Flush();
  return pages;
 }
 TextLabel ArticleBody(string markup,int x,int y,int w,int h)
 {
  var body=Label(markup,x,y,w,h,25,ink,"Medium");body.MultiLine=true;body.LineWrapMode=LineWrapMode.Word;body.VerticalAlignment=VerticalAlignment.Top;body.EnableMarkup=true;return body;
 }
 void BuildNewsDetail()
 {
  var article=SelectedNewsArticle();
  if(article==null){BuildNewsDropdown();return;}
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.02f,.03f,.06f,.86f)});
  int pw=1620,ph=960;
  var panel=Panel(150,60,pw,ph,Sf(.99f,.99f,1,1),30);
  ArticleFull full;bool ready=articleCache.TryGetValue(article.Url,out full);
  bool loading=!ready&&articlePending==article.Url&&(DateTime.Now-articlePendingAt).TotalSeconds<15;
  var pages=ArticlePages(article);newsDetailPage=Math.Max(0,Math.Min(newsDetailPage,pages.Count-1));
  string category=newsCategoryIndex>=0&&newsCategoryIndex<newsSectionNames.Length?newsSectionNames[newsCategoryIndex]:"Actualités";
  if(newsDetailPage==0){
   string image=ready&&full.Image!=""?full.Image:article.Image;
   if(image!="")panel.Add(new ImageView{ResourceUrl=image,Position2D=new Position2D(40,40),Size2D=new Size2D(720,405),FittingMode=FittingModeType.ScaleToFill,DesiredWidth=1440,DesiredHeight=810,CornerRadius=new Vector4(20,20,20,20)});
   else panel.Add(new View{Position2D=new Position2D(40,40),Size2D=new Size2D(720,405),BackgroundColor=Sf(.93f,.94f,.97f,1),CornerRadius=new Vector4(20,20,20,20)});
   panel.Add(Label(category.ToUpperInvariant()+" · "+article.Source,800,44,780,30,18,accent,"Semibold"));
   var title=Label(article.Title,800,84,780,250,38,ink,"Display");title.MultiLine=true;title.LineWrapMode=LineWrapMode.Word;title.VerticalAlignment=VerticalAlignment.Top;panel.Add(title);
   if(article.Published!="")panel.Add(Label("Publié le "+article.Published.Replace(" "," à "),800,350,780,28,18,muted,"Medium"));
   int words=0;if(ready)foreach(var block in full.Blocks)words+=block.Split(' ').Length;
   string status=loading?"Chargement de l’article complet…":ready&&full.Blocks.Count>0?"Lecture "+Math.Max(1,(int)Math.Round(words/230.0))+" min · "+pages.Count+" page"+(pages.Count>1?"s":""):"Article complet indisponible · résumé affiché";
   panel.Add(Label(status,800,386,780,28,18,loading?accent:muted,"Semibold"));
   panel.Add(ArticleBody(pages[0],40,476,1540,400));
  }else{
   string head=category.ToUpperInvariant()+" · "+(article.Title.Length>110?article.Title.Substring(0,107)+"…":article.Title);
   panel.Add(Label(head,40,34,1540,34,19,muted,"Semibold"));
   panel.Add(new View{Position2D=new Position2D(40,80),Size2D=new Size2D(1540,2),BackgroundColor=Sf(.90f,.91f,.94f,1)});
   panel.Add(ArticleBody(pages[newsDetailPage],40,104,1540,772));
  }
  panel.Add(new View{Position2D=new Position2D(40,ph-66),Size2D=new Size2D(1540,2),BackgroundColor=Sf(.90f,.91f,.94f,1)});
  panel.Add(Label((pages.Count>1?"Page "+(newsDetailPage+1)+" / "+pages.Count+" · ←/→ pour tourner les pages · ":"")+"Retour aux titres",40,ph-56,1100,36,18,muted,"Medium"));
  var src=Label("Source : "+article.Source,1180,ph-56,400,36,18,muted,"Medium");src.HorizontalAlignment=HorizontalAlignment.End;panel.Add(src);
 }
 void OpenNewsMenu(int category)
 {
  newsReturnSelected=selected;newsReturnArea=area;newsCategoryIndex=Math.Max(0,Math.Min(newsSectionNames.Length-1,category));newsArticleIndex=0;
  newsMenuOpen=true;newsDetailOpen=false;area=7;BuildPage();selected=newsCategorySlotStart+newsCategoryIndex;UpdateFocus(false);
  if(DateTime.Now-lastNewsRequest>=TimeSpan.FromMinutes(30))RequestNews();
  WriteState("news_menu_opened:"+newsSectionNames[newsCategoryIndex]);
 }
 void CloseNewsMenu()
 {
  newsMenuOpen=false;newsDetailOpen=false;quoteDetailOpen=false;area=newsReturnArea;selected=newsReturnSelected;BuildPage();WriteState("news_menu_closed");
 }
 bool HandleNewsKey(string key)
 {
  bool back=key=="XF86Back"||key=="Back"||key=="Escape"||key=="XF86Exit";
  bool confirm=key=="Return"||key=="KP_Enter";
  if(quoteDetailOpen){
   if(back||confirm){quoteDetailOpen=false;area=11;BuildPage();selected=quoteSlotStart+quoteIndex;UpdateFocus(false);return true;}
   if(key=="Left"||key=="Right"){quoteRange=(quoteRange+(key=="Left"?-1:1)+rangeIds.Length)%rangeIds.Length;BuildPage();return true;}
   if(key=="Up"||key=="Down"){int n=QuoteCount();quoteIndex=(quoteIndex+(key=="Up"?-1:1)+n)%n;BuildPage();return true;}
   return true;
  }
  if(back){
   if(newsDetailOpen){newsDetailOpen=false;area=8;selected=newsArticleSlotStart+newsArticleIndex;BuildPage();WriteState("news_detail_closed");}
   else CloseNewsMenu();
   return true;
  }
  if(newsDetailOpen){
   int pageDirection=key=="Up"||key=="Left"||key=="Page_Up"?-1:key=="Down"||key=="Right"||key=="Page_Down"||confirm?1:0;
   if(pageDirection!=0){
    int pageCount=ArticlePages(SelectedNewsArticle()).Count;
    int next=Math.Max(0,Math.Min(pageCount-1,newsDetailPage+pageDirection));
    if(next!=newsDetailPage){newsDetailPage=next;BuildPage();area=9;UpdateFocus(false);WriteState("news_detail_page:"+newsDetailPage);}
   }
   return true;
  }
  int direction=key=="Left"?-1:key=="Right"?1:0;
  if(area==7){
   if(direction!=0){newsCategoryIndex=(newsCategoryIndex+direction+newsSectionNames.Length)%newsSectionNames.Length;quoteDetailOpen=false;BuildPage();selected=newsCategorySlotStart+newsCategoryIndex;UpdateFocus(false);return true;}
   if(key=="Down"||confirm){
    if(quoteSlotStart>=0){area=11;quoteIndex=0;selected=quoteSlotStart;UpdateFocus(false);return true;}
    var articles=NewsItems(newsCategoryIndex);
    if(articles.Count>0){area=8;newsArticleIndex=0;selected=newsArticleSlotStart;UpdateFocus(false);}
    return true;
   }
   return true;
  }
  if(area==11){
   int n=QuoteCount(),perRow=QuotePerRow(),row=quoteIndex/perRow,col=quoteIndex%perRow,rows=(n+perRow-1)/perRow;
   if(direction!=0){int start=row*perRow,len=Math.Min(perRow,n-start);quoteIndex=start+(col+direction+len)%len;selected=quoteSlotStart+quoteIndex;UpdateFocus(false);return true;}
   if(key=="Up"){if(row>0){quoteIndex=Math.Min(n-1,quoteIndex-perRow);selected=quoteSlotStart+quoteIndex;}else{area=7;selected=newsCategorySlotStart+newsCategoryIndex;}UpdateFocus(false);return true;}
   if(key=="Down"){if(row<rows-1){quoteIndex=Math.Min(n-1,quoteIndex+perRow);selected=quoteSlotStart+quoteIndex;}else if(NewsItems(newsCategoryIndex).Count>0){area=8;newsArticleIndex=0;selected=newsArticleSlotStart;}UpdateFocus(false);return true;}
   if(confirm){quoteDetailOpen=true;BuildPage();WriteState("quote_opened:"+marketQuotes[quoteIndex].Symbol);if(DateTime.Now-lastMarketsRequest>=TimeSpan.FromSeconds(60)){lastMarketsRequest=DateTime.Now;SendDataRequest(false,false,false,false,false,true);}return true;}
   return true;
  }
  if(area==8){
   if(key=="Up"){if(quoteSlotStart>=0){int n=QuoteCount();quoteIndex=((n-1)/QuotePerRow())*QuotePerRow();area=11;selected=quoteSlotStart+quoteIndex;}else{area=7;selected=newsCategorySlotStart+newsCategoryIndex;}UpdateFocus(false);return true;}
   if(direction!=0){newsCategoryIndex=(newsCategoryIndex+direction+newsSectionNames.Length)%newsSectionNames.Length;newsArticleIndex=0;area=8;BuildPage();if(NewsItems(newsCategoryIndex).Count>0)selected=newsArticleSlotStart;else{area=7;selected=newsCategorySlotStart+newsCategoryIndex;}UpdateFocus(false);return true;}
   var articles=NewsItems(newsCategoryIndex);int count=Math.Min(ArticleLimit(),articles.Count);
   if(key=="Down"&&count>0){newsArticleIndex=(newsArticleIndex+1)%count;selected=newsArticleSlotStart+newsArticleIndex;UpdateFocus(false);return true;}
   if(confirm&&count>0){newsArticleIndex=Math.Max(0,selected-newsArticleSlotStart);newsDetailPage=0;newsDetailOpen=true;area=9;RequestArticle(SelectedNewsArticle());BuildPage();UpdateFocus(false);WriteState("news_detail_opened");return true;}
   return true;
  }
  return true;
 }
 List<int> Row(int row){var r=new List<int>();for(int i=0;i<slots.Count;i++)if(slots[i].Row==row)r.Add(i);return r;}
 // ---- BelAir OS 2 design tokens: one glass material, one focus language ----
 string PageTitle(){return page==1?"Applis":page==2?"Réglages":page==3?"Météo":page==4?"Trafic":page==6?"Système":page==7?"Énergie":page==8?"Plex":page==9?"Félix":page==10?"Vélos":page==11?"Localiser":"";}
 // light "liquid glass": what is behind the control is blurred, the control stays translucent
 static void LiquidGlass(View v,float radius=22f){try{v.SetRenderEffect(RenderEffect.CreateBackgroundBlurEffect(radius));}catch{}}
 Color GlassFill(){return dark?new Color(1,1,1,.10f):prefWall==4?new Color(1f,.985f,.96f,.9f):new Color(1,1,1,.66f);}
 Color GlassEdge(){return dark?new Color(1,1,1,.20f):new Color(0,0,0,.07f);}
 Color FocusEdge(){return dark?new Color(1,1,1,.88f):new Color(1,1,1,1);}
 readonly Dictionary<View,Color> restBg=new Dictionary<View,Color>();
 Shadow FocusShadow(bool on){return on?new Shadow(50f,new Color(0,0,0,dark?.55f:.30f),new Vector2(0,22)):new Shadow(0f,new Color(0,0,0,0),new Vector2(0,0));}
 void UpdateFocus(bool animate=true)
 {
  if(focusAnimation!=null){focusAnimation.Stop();focusAnimation.Dispose();focusAnimation=null;}
  if(animate)focusAnimation=new Animation(180);
  // tab bar: the current page is a soft capsule; while the bar has focus it turns into a solid capsule (white in dark, accent in light)
  for(int i=0;i<tabs.Length;i++){bool act=tabPage[i]==page,hot=act&&area==0;
   tabs[i].TextColor=hot?(dark?new Color(.04f,.06f,.10f,1):white):act?ink:muted;tabs[i].FontFamily="Quiet Inter "+(act?"Semibold":"Medium");
   tabs[i].BackgroundColor=hot?(dark?white:accent):act?(dark?new Color(1,1,1,.16f):new Color(0,0,0,.07f)):new Color(0,0,0,0);tabs[i].CornerRadius=new Vector4(28,28,28,28);}
  for(int i=0;i<slots.Count;i++){
   var s=slots[i];bool newsControl=s.Action.StartsWith("news_category:")||s.Action.StartsWith("news_item:")||s.Action.StartsWith("quote:");
   // PIN pad: focus is shown only on its starting key (1), so you know where you are but nobody can follow the code.
   bool f=factDetailOpen?false:page==11&&!findUnlocked?selected==i&&slots[i].Action=="pin:1":page==3?area==3&&selected==i:newsMenuOpen?newsControl&&area!=9&&!quoteDetailOpen&&selected==i:area!=0&&area!=9&&selected==i;
   float scale=f?(page==8||page==9||page==10||page==11?(s.H>120?1.06f:1.05f):page==2&&s.Row==2?1.04f:page==5&&s.Action.StartsWith("ha_lamp:")?1.08f:page==5||page==4?1.05f:s.Action=="news"?1.035f:newsControl?1.015f:page==1&&s.Row>=0?1.055f:page==2&&s.Row>=0?1.035f:s.Row==0?1.05f:s.Row==1?(page==0?1.1f:1.03f):s.Row==-2?(page==0?1.07f:1.025f):s.Row==-1?1.18f:1):1;
   // focus = lift (scale + soft shadow) and a thin light edge, never a thick coloured outline
   bool edgeAtRest=page==1&&s.Row>=0||s.Row==0&&page==0||s.Row==-1;
   s.Card.BorderlineWidth=f?2.5f:edgeAtRest?1:0;
   s.Card.BorderlineColor=f?FocusEdge():GlassEdge();
   // glass cards light up when selected (relief, not a coloured outline)
   if(!restBg.ContainsKey(s.Card)){var bg=s.Card.BackgroundColor;var gf=GlassFill();if(bg!=null&&Math.Abs(bg.A-gf.A)<.01f&&Math.Abs(bg.R-gf.R)<.01f&&Math.Abs(bg.G-gf.G)<.01f&&Math.Abs(bg.B-gf.B)<.01f)restBg[s.Card]=gf;}
   if(restBg.TryGetValue(s.Card,out var rb))s.Card.BackgroundColor=f?(dark?new Color(1,1,1,.24f):new Color(1,1,1,1)):rb;
   s.Card.BoxShadow=FocusShadow(f);
   var pos=new Position(s.X+s.W/2f,s.Y+s.H/2f-(f&&(s.Row==0||(page==1||page==8)&&s.Row>=0)?5:0),0);
   if(animate){focusAnimation.AnimateTo(s.Card,"Scale",new Vector3(scale,scale,1));focusAnimation.AnimateTo(s.Card,"Position",pos);}else{s.Card.Scale=new Vector3(scale,scale,1);s.Card.Position=pos;}
   if(s.Shadow!=null)s.Shadow.Opacity=f?1:.55f;
   if(s.FocusRing!=null)s.FocusRing.Opacity=f?1:0;
   if(s.Label!=null&&page!=2)s.Label.FontFamily="Quiet Inter "+(f?"Semibold":"Medium");
   if(f&&!coverPopupOpen&&!lightOpen&&!pictureOpen&&!cleanOpen&&!lineDetailOpen&&!forecastDetailOpen&&!newsDetailOpen&&!factDetailOpen)s.Card.RaiseToTop();
  }
  if(page==5)foreach(var id in haLampId)SetLampVisual(id);
  if(page==0&&homeShelf==1)UpdateInfoHero();
  for(int i=0;i<hourCards.Count;i++){
   bool focused=page==3&&area==5&&i==forecastHourFocus;
   hourCards[i].BorderlineWidth=focused?4:0;hourCards[i].BorderlineColor=accent;
  }
  for(int i=0;i<dayCards.Count;i++){
   bool focused=page==3&&area==6&&i==forecastDayFocus;
   dayCards[i].BorderlineWidth=focused?4:0;dayCards[i].BorderlineColor=accent;
  }
  if(focusAnimation!=null)focusAnimation.Play();
  bool onToolbar=area==3&&selected>=0&&selected<slots.Count&&slots[selected].Row==-1;if(clock!=null)clock.Opacity=page==3||page==4||page!=0&&onToolbar?0:1;toolbarHint.Text=onToolbar&&!lightOpen?slots[selected].Name:"";
  detail.Text=quoteDetailOpen?"←/→ échelle · ↑/↓ action précédente ou suivante · Retour pour fermer":area==11?"←/→ choisir · OK graphique du jour · ↑ rubriques · ↓ articles":page==3&&area==5?"←/→ heures · ↓ jours · OK détail · ↑ menu":page==3&&area==6?"←/→ jours · ↑ heures · OK détail":area==7?"←/→ rubrique · ↓ titres · OK pour afficher":area==8?"↑ rubriques · ←/→ changer · OK pour le détail":page==0&&area==4&&selected==metroSlotIndex?"OK pour voir les 2 prochains métros par direction":page==0&&area==4&&selected==weatherSlotIndex?"OK pour les prévisions du jour et sur 7 jours":(page==1||page==8||page==9||page==10||page==11)&&area==1&&slots[selected].Row>=0?(slots[selected].Name!=""?slots[selected].Name+" · ":"")+slots[selected].Description:area==0&&page==0?"↑ Actualités · gauche/droite pour changer d’écran":area==0?"":area==2?slots[selected].Name:"";
  if(page==2){
   slots[0].Label.Text=currentVolume<0?"Indisponible":currentVolume.ToString();
   slots[1].Label.Text=SettingName("brightness");
   slots[2].Label.Text=SettingName("contrast");
   slots[3].Label.Text=SettingName("picturemode");
   slots[4].Label.Text=SettingName("game-mode");
   slots[5].Label.Text=currentMute<0?"Indisponible":currentMute==1?"Muet":"Son activé";
   slots[6].Label.Text=SettingName("sleeptimer");
   slots[7].Label.Text="Connexions";
   slots[8].Label.Text="Samsung";
   slots[9].Label.Text="Associer";
   slots[10].Label.Text=prefTheme=="dark"?"Sombre":prefTheme=="light"?"Clair":"Automatique";
   slots[11].Label.Text=accentNames[prefAccent];
   slots[12].Label.Text=wallNames[prefWall];
   slots[13].Label.Text=prefBrief?"Activé":"Désactivé";
   detail.Text=area==10?slots[selected].Description+" · OK pour changer":"Commandes rapides";
  }
  if(editing!="")detail.Text=editing.StartsWith("covpos:")?"←/→ pour régler le volet · OK ou Retour pour terminer":"Gauche / droite pour régler "+(editing=="volume"?"le volume":editing=="brightness"?"la luminosité (paliers de 5)":editing=="contrast"?"le contraste (paliers de 5)":editing)+" · OK ou Retour pour terminer";
  ambientRoot?.RaiseToTop();briefRoot?.RaiseToTop();
  WriteState("focus");
 }
 void OnKey(object sender,Window.KeyEventArgs e)
 {
  if(e.Key!=null&&e.Key.State==Key.StateType.Up){
   string ku=e.Key.KeyPressedName;keyUpSeen=true;
   if(ku=="Return"||ku=="KP_Enter"){swallowReturn=false;if(pendingLamp!=null){string id=pendingLamp;pendingLamp=null;longPressTimer?.Stop();ToggleLamp(id);}if(pendingFind!=null){string id=pendingFind;pendingFind=null;longPressTimer?.Stop();FocusFind(id);}}
   return;
  }
  if(e.Key==null||e.Key.State!=Key.StateType.Down)return;string k=e.Key.KeyPressedName;
  if((k=="Return"||k=="KP_Enter")&&(swallowReturn||pendingLamp!=null||pendingFind!=null))return; // OK still held after a long pressisPaused=false;lastInput=DateTime.Now;if(ambientRoot!=null){CloseAmbient();return;}
  long now=Environment.TickCount64;if(k==lastKey&&now-lastKeyAt<180)return;lastKey=k;lastKeyAt=now;
  if(briefRoot!=null){CloseBrief(true);return;}
  if(MaybeBrief())return;
  try{
   if(k=="XF86Home"){Home("home_key");return;}
   if(k=="XF86SysMenu"||k=="XF86More"){OpenSettings("settings_key");return;}
   if(factDetailOpen){if(k=="Return"||k=="KP_Enter"){RotateFact();return;}if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){factDetailOpen=false;BuildPage();WriteState("fact_detail_closed");return;}return;}
   if(newsMenuOpen){HandleNewsKey(k);return;}
   if(coverPopupOpen){HandleCoverPopupKey(k);return;}
   if(lightOpen&&catalogOpen){HandleCatalogKey(k);return;}
   if(lightOpen){HandleLightKey(k);return;}
   if(appearOpen){HandleAppearanceKey(k);return;}
   if(pictureOpen){HandlePictureKey(k);return;}
   if(cleanOpen){HandleCleanKey(k);return;}
   if(page==4){
    if(lineDetailOpen){if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"||k=="Return"||k=="KP_Enter"){lineDetailOpen=false;BuildPage();}return;}
    if(HandleGridKey(k,"metro_back"))return;
    if(k=="Up"||k=="Down")return;
   }
   if(page==11&&!findUnlocked&&k.Length==1&&k[0]>='0'&&k[0]<='9'){PinKey(k);return;}
   if(page==11&&ringAsk!=""){HandleRingKey(k);return;}
   if(page==11&&findUnlocked&&findPan){
    if(k=="Return"||k=="KP_Enter"||k=="XF86Back"||k=="Back"||k=="Escape"){findPan=false;RebuildKeepFocus();return;}
    if(k=="Up"||k=="Down"||k=="Left"||k=="Right"){FindPanStep(k);return;}
   }
   if(page==11&&findUnlocked&&(k=="XF86RaiseChannel"||k=="XF86LowerChannel"||k=="KP_Add"||k=="KP_Subtract"||k=="plus"||k=="minus")){FindZoom(k=="XF86RaiseChannel"||k=="KP_Add"||k=="plus"?1:-1);return;}
   if(page==11&&findUnlocked&&findFocus!=""&&(k=="XF86Back"||k=="Back"||k=="Escape")){findFocus="";findZoom=-1;panWx=0;panWy=0;findPan=false;RebuildKeepFocus();return;}
   if(page==10&&mapSel>=0){HandleMapKey(k);return;}
   if(page==10&&area==1&&k=="Right"&&velib.Count+limePts.Count>0){EnterMap();return;}
   if((page==8||page==9||page==10||page==11)&&HandleGridKey(k,page==8?"plex_back":"felix_back"))return;
   if(page==2&&editing==""&&(area==1||area==2||area==10)&&(k=="Up"||k=="Down"||k=="Left"||k=="Right")){
    int row=area==10?2:area-1;var r=Row(row);int col=Math.Max(0,r.IndexOf(selected));
    if(k=="Left"||k=="Right"){selected=r[(col+(k=="Left"?-1:1)+r.Count)%r.Count];UpdateFocus();return;}
    int nr=row+(k=="Up"?-1:1);
    if(nr<0){area=0;UpdateFocus();return;}
    if(nr>2||Row(nr).Count==0)return;
    var target=Row(nr);selected=target[Math.Min(col,target.Count-1)];area=nr==2?10:nr+1;UpdateFocus();return;
   }
   if(page==7){
    if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){Home("energy_back");return;}
    if(k=="Return"||k=="KP_Enter"){RequestEnergy();return;}
    if(k=="Up"||k=="Down")return;
   }
   if(page==6){
    if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){Home("system_back");return;}
    if(k=="Return"||k=="KP_Enter"){CollectSystemInfo();BuildPage();return;}
    if(k=="Up"||k=="Down")return;
   }
   if(page==5&&editing==""&&(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit")){Home("ha_back");return;}
   if(page==3){
    if(forecastDetailOpen){
     if(k=="Return"||k=="KP_Enter"||k=="XF86Back"||k=="Back"||k=="Escape"){
      forecastDetailOpen=false;selectedForecastHour=-1;selectedForecastDay=-1;BuildPage();WriteState("weather_detail_closed");return;
     }
     return;
    }
    if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){Home("weather_back");return;}
    if(area==5){
     if(k=="Up"){area=3;selected=1;UpdateFocus();return;}
     if(k=="Down"&&dayCards.Count>0){area=6;forecastDayFocus=0;UpdateFocus();return;}
     if(k=="Left"||k=="Right"){ScrollForecastHours(k);return;}
     if(k=="Return"||k=="KP_Enter"){
      if(forecastHours.Count>0){selectedForecastHour=forecastHourOffset+forecastHourFocus;selectedForecastDay=-1;forecastDetailOpen=true;BuildPage();WriteState("weather_hour_detail_opened");}
      return;
     }
     return;
    }
    if(area==6){
     if(k=="Up"){area=5;UpdateFocus();return;}
     if(k=="Left"||k=="Right"){
      int direction=k=="Left"?-1:1;forecastDayFocus=(forecastDayFocus+direction+dayCards.Count)%dayCards.Count;UpdateFocus();return;
     }
     if(k=="Return"||k=="KP_Enter"){
      if(forecastDayFocus<forecast.Count){selectedForecastDay=forecastDayFocus;selectedForecastHour=-1;forecastDetailOpen=true;BuildPage();WriteState("weather_day_detail_opened");}
      return;
     }
     return;
    }
    if(k=="Down"&&area==3){area=5;forecastHourFocus=0;UpdateFocus();return;}
    if(k=="Up"||k=="Down")return;
   }
   if(page==1&&HandleLibraryKey(k))return;
   if(page==0&&area==0&&k=="Up"&&newsButtonSlotIndex>=0){area=3;selected=newsButtonSlotIndex;UpdateFocus();return;}
   if(page==0&&area==3&&selected==newsButtonSlotIndex&&k=="Down"){area=0;selected=page;UpdateFocus();return;}
   if(editing!=""){if(k=="Left"||k=="Right"){int d=k=="Left"?-1:1;if(editing=="volume")ChangeVolume(d);else if(editing=="brightness"||editing=="contrast")ChangeLevel(editing,d);else if(editing.StartsWith("covpos:"))ChangeCover(editing.Substring(7),d);}else if(k=="Return"||k=="KP_Enter"||k=="XF86Back"||k=="Back"||k=="Escape"){editing="";notice.Text="";UpdateFocus();}return;}
   if(page==5){
    if(k=="Return"||k=="KP_Enter"){if(area==0){area=1;selected=Row(0).Count>0?Row(0)[0]:0;UpdateFocus();}else Activate();return;}
    if(k=="Up"){if(area<=1){area=0;}else{int nr=area-2;selected=NearestInRow(nr,slots[selected].X);area=nr+1;}UpdateFocus();return;}
    if(k=="Down"){if(area==0){area=1;selected=Row(0).Count>0?Row(0)[0]:0;}else{int nr=area;if(Row(nr).Count>0){selected=NearestInRow(nr,slots[selected].X);area=nr+1;}}UpdateFocus();return;}
    if(k=="Left"||k=="Right"){int d=k=="Left"?-1:1;if(area==0){int ti=Array.IndexOf(tabPage,page)+d;if(ti>=0&&ti<tabPage.Length){page=tabPage[ti];selected=0;BuildPage();}}else{var r=Row(area-1);int n=r.IndexOf(selected);if(n>=0){selected=r[(n+d+r.Count)%r.Count];UpdateFocus();}}return;}
    return;
   }
   if(HandleHomeNavigation(k))return;
   if(k=="Up"){if(area==2){int col=Row(1).IndexOf(selected);area=1;selected=Row(0)[page==2?col:0];}else if(area==1||area==4)area=0;UpdateFocus();}
   else if(k=="Down"){if(area==4){area=1;selected=Row(0)[0];}else if(area==0||area==3){area=1;selected=Row(0)[0];}else if(area==1){int col=Row(0).IndexOf(selected);area=2;selected=Row(1)[page==2?col:0];}UpdateFocus();}
   else if(k=="Left"||k=="Right"){
    int d=k=="Left"?-1:1;
    if(area==4){var widgets=Row(-2);int n=widgets.IndexOf(selected)+d;if(n<0||n>=widgets.Count)return;selected=widgets[n];UpdateFocus();}
    else if(area==0){int ti=Array.IndexOf(tabPage,page)+d;if(ti<0||ti>=tabPage.Length){area=3;selected=Row(-1)[ti<0?2:0];UpdateFocus();}else{page=tabPage[ti];selected=0;BuildPage();}}
    else if(area==3){var r=Row(-1);int n=r.IndexOf(selected)+d;if(n<0||n>=r.Count){area=0;page=n<0?2:0;selected=0;BuildPage();}else{selected=r[n];UpdateFocus();}}
    else{var r=Row(area-1);int n=r.IndexOf(selected);selected=r[(n+d+r.Count)%r.Count];UpdateFocus();}
   }
   else if(k=="Return"||k=="KP_Enter"){if(area==0){area=1;selected=0;UpdateFocus();}else Activate();}
   else if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit")SelectSource(lastSource);
  }catch(Exception ex){ShowError(ex.Message);}
 }
 // Accueil rows: hero button (area 1, Row 0), "Regarder" dock (area 2, Row 1), "Infos" tiles (area 4, Row -2).
 bool HandleHomeNavigation(string key)
 {
  if(page!=0||key!="Up"&&key!="Down"&&key!="Left"&&key!="Right")return false;
  if(area==1&&(key=="Left"||key=="Right")){var r=Row(0);int n=r.IndexOf(selected)+(key=="Left"?-1:1);if(n>=0&&n<r.Count){selected=r[n];UpdateFocus();}return true;}
  if(area==2&&key=="Down"){int x=slots[selected].X;homeShelf=1;selected=0;area=4;BuildPage();selected=NearestInRow(-2,x);UpdateFocus(false);return true;}
  if(area==4&&key=="Up"){int x=slots[selected].X;homeShelf=0;selected=0;area=2;BuildPage();selected=NearestInRow(1,x);UpdateFocus(false);return true;}
  if(area==4&&key=="Down"){if(Row(-3).Count>0){area=12;selected=NearestInRow(-3,slots[selected].X);UpdateFocus();}return true;}
  if(area==12&&key=="Up"){area=4;selected=NearestInRow(-2,slots[selected].X);UpdateFocus();return true;}
  if(area==12&&(key=="Left"||key=="Right")){var r=Row(-3);int n=r.IndexOf(selected)+(key=="Left"?-1:1);if(n>=0&&n<r.Count){selected=r[n];UpdateFocus();}return true;}
  if(area==12&&key=="Down")return true;
  return false;
 }
 bool HandleGridKey(string k,string back)
 {
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){Home(back);return true;}
  if(area==0){if(k!="Down")return false;if(Row(0).Count>0){area=1;selected=Row(0)[0];UpdateFocus();}return true;}
  if(area==3)return false;
  if(k=="Return"||k=="KP_Enter"){if(selected>=0&&selected<slots.Count&&slots[selected].Row>=0)Activate();return true;}
  if(k!="Up"&&k!="Down"&&k!="Left"&&k!="Right")return false;
  int cr=selected>=0&&selected<slots.Count?slots[selected].Row:0;
  if(k=="Up"){if(cr>0&&Row(cr-1).Count>0)selected=NearestInRow(cr-1,slots[selected].X);else area=0;UpdateFocus();return true;}
  if(k=="Down"){if(Row(cr+1).Count>0)selected=NearestInRow(cr+1,slots[selected].X);UpdateFocus();return true;}
  var r=Row(cr);int n=r.IndexOf(selected);if(n>=0)selected=r[(n+(k=="Left"?-1:1)+r.Count)%r.Count];UpdateFocus();return true;
 }
 bool HandleLibraryKey(string key)
 {
  if(key=="XF86Back"||key=="Back"||key=="Escape"||key=="XF86Exit"){Home("apps_back");return true;}
  if(library.Count==0)return key=="Up"||key=="Down";
  if((area==0||area==3)&&key=="Down"){area=1;selected=0;UpdateFocus();return true;}
  if(area!=1)return false;
  if(key=="Left"||key=="Right"||key=="Up"||key=="Down"){
   int next=AppLibrary.Move(library.Count,libraryPage*AppLibrary.PageSize+selected,key);
   if(next<0){area=0;UpdateFocus();return true;}
   int nextPage=next/AppLibrary.PageSize;selected=next%AppLibrary.PageSize;
   if(nextPage!=libraryPage){libraryPage=nextPage;BuildPage();}else UpdateFocus();
   return true;
  }
  return false;
 }
 void Activate()
 {
  string a=slots[selected].Action;notice.Text="";
  if(a=="news"){OpenNewsMenu(0);return;}
  if(a=="markets"){OpenNewsMenu(3);return;}
  if(a=="fact"){factDetailOpen=true;BuildPage();WriteState("fact_detail_opened");return;}
  if(a=="weather"){OpenForecast();return;}
  if(a=="metro"){OpenMetroDetail();return;}
  if(a.StartsWith("line:")){lineDetailKey=a.Substring(5);lineDetailOpen=true;BuildPage();return;}
  if(a.StartsWith("weather_day:")){WriteState("weather_day:"+a.Substring(12));return;}
  if(a.StartsWith("hdmi:")){SelectSource(int.Parse(a.Substring(5)));return;}
  if(a.StartsWith("page:")){page=int.Parse(a.Substring(5));selected=page==0?SourceIndex():0;area=1;BuildPage();return;}
  if(a.StartsWith("app:")){BumpUsage(a.Substring(4));Launch(a.Substring(4));return;}
  if(a.StartsWith("channel:")){StartChannel(a.Substring(8));return;}
  if(a.StartsWith("atv:")){LaunchOnAppleTv(a);return;}
  if(a.StartsWith("pin:")){PinKey(a.Substring(4));return;}
  if(a.StartsWith("findzoom:")){FindZoom(a.EndsWith("in")?1:-1);return;}
  if(a=="findpan"){findPan=!findPan;RebuildKeepFocus();return;}
  if(a.StartsWith("find:")){
   string fid=a.Substring(5);
   if(!keyUpSeen){FocusFind(fid);return;}
   pendingFind=fid;longPressTimer?.Stop();longPressTimer=new Tizen.NUI.Timer(600);
   longPressTimer.Tick+=(o,ev)=>{if(pendingFind!=null){string id=pendingFind;pendingFind=null;swallowReturn=true;var it=findItems.FirstOrDefault(f=>f.Id==id);if(it!=null&&it.Sound==""){notice.Text="Cet appareil ne peut pas sonner.";}else if(it!=null){ringAsk=id;ringSel=0;RebuildKeepFocus();}}return false;};
   longPressTimer.Start();return;
  }
  if(a.StartsWith("light:")){lightId=a.Substring(6);lightOpen=true;catalogOpen=false;lightRow=0;lightCol=0;lightScroll=0;BuildPage();return;}
  if(a.StartsWith("ha_scene:")){string sc=a.Substring(9);if(sc=="scene.film"||sc=="scene.pause"){bool kon=sc=="scene.pause";string svc=kon?"turn_on":"turn_off";haLampState[cuisineGroup]=kon?"on":"off";SendHaBatch("[{\"d\":\"scene\",\"s\":\"turn_on\",\"e\":\""+sc+"\"},{\"d\":\"light\",\"s\":\""+svc+"\",\"e\":\""+cuisineGroup+"\"}]");SetLampVisual(cuisineGroup);}else SendHaAction("scene","turn_on",sc);notice.Text="Scène activée";return;}
  if(a.StartsWith("ha_cover:")){coverPopupEntity=a.Substring(9);int cp=CoverPos(coverPopupEntity);int[] prs={0,25,50,75,100};coverPopupSel=0;for(int i=1;i<prs.Length;i++)if(Math.Abs(prs[i]-cp)<Math.Abs(prs[coverPopupSel]-cp))coverPopupSel=i;coverPopupOpen=true;notice.Text="";BuildPage();return;}
  if(a.StartsWith("ha_vac:")){string svc=a.Substring(7);SendHaAction("vacuum",svc,"vacuum.felix");string msg=svc=="start"?"Démarrage…":svc=="pause"?"Mise en pause…":"Retour à la base…";if(felixState!=null)felixState.Text=msg;notice.Text="Félix · "+msg;FelixSoon();return;}
  if(a=="felix_clean"){cleanOpen=true;cleanRow=0;cleanCol=0;if(felixRooms.Count==0)RequestExtra(false,false,true);BuildPage();return;}
  if(a.StartsWith("ha_btn:")){SendHaAction("button","press",a.Substring(7));notice.Text="Félix · programme « "+slots[selected].Name+" » lancé";FelixSoon();return;}
  if(a.StartsWith("plex_play:")){string id=a.Substring(10);foreach(var it in plexItems)if(it.Id==id){PlayPlex(it);return;}ShowError("Ce titre n’est plus dans la liste Plex.");return;}
  if(a=="now_playing"){if(nowPlaying!=null&&nowPlaying.Source=="Plex"){BumpUsage("kIciSQlYEM.plex");Launch("kIciSQlYEM.plex");}else SelectSource(13);return;}
  if(a=="pref:theme"||a=="pref:wall"||a=="pref:accent"){appearOpen=true;appearRow=a=="pref:theme"?0:a=="pref:wall"?1:2;appearCol=AppearCurrent(appearRow);if(appearRow==1&&plexItems.Count==0)RequestExtra(true,true,false);BuildPage();return;}
  if(a.StartsWith("pref:")){CyclePref(a.Substring(5));return;}
  if(a.StartsWith("ha_auto:")){string id=a.Substring(8);bool on=SensorState(id)=="off";haSensor[id]=on?"on":"off";SendHaAction("automation",on?"turn_on":"turn_off",id);UpdateDevices();notice.Text=on?"Mode cinéma automatique activé":"Mode cinéma automatique coupé";return;}
  if(a.StartsWith("ha_lamp:")&&colorLights.Contains(a.Substring(8))){lightId=a.Substring(8);lightOpen=true;lightRow=0;lightCol=0;BuildPage();return;}
  if(a.StartsWith("ha_lamp:")&&dimLamps.Contains(a.Substring(8))&&keyUpSeen){
   pendingLamp=a.Substring(8);longPressTimer?.Stop();longPressTimer=new Tizen.NUI.Timer(550);
   longPressTimer.Tick+=(o,ev)=>{if(pendingLamp!=null){lightId=pendingLamp;pendingLamp=null;swallowReturn=true;lightOpen=true;lightRow=0;lightCol=NearestBriStep(lightId);BuildPage();}return false;};
   longPressTimer.Start();return;
  }
  if(a.StartsWith("ha_lamp:")){ToggleLamp(a.Substring(8));return;}
  if(a=="picturemode"){OpenPicture();return;}
  if(a=="controllers"){Launch("org.tizen.menu","add-bluetooth-keyboard");return;}
  if(a=="network"){Launch("org.tizen.menu","networksettings");return;}
  if(a=="volume"||a=="brightness"||a=="contrast"){RefreshSettings();if(a!="volume"&&!CanEdit(a)){ShowError((a=="brightness"?"Luminosité":"Contraste")+" indisponible sur cette entrée.");return;}editing=a;UpdateFocus();return;}
  if(a=="game-mode"){RefreshSettings();SetSetting(a,SettingValue(a)=="1"?"0":"1");return;}
  if(a=="sleeptimer"){RefreshSettings();int n;if(int.TryParse(SettingValue(a),out n))SetSetting(a,((n+1)%5).ToString());return;}
  if(a=="mute"){int n;int r=sound_manager_get_master_mute(out n);if(r==0)r=sound_manager_set_master_mute(n==1?0:1);if(r!=0)ShowError("Utilisez la touche Muet de la télécommande.");RefreshSettings();UpdateFocus();WriteState("mute_changed");}
 }
 void OpenForecast()
 {
  page=3;area=3;selected=1;editing="";forecastDetailOpen=false;selectedForecastHour=-1;selectedForecastDay=-1;forecastHourOffset=0;forecastHourFocus=0;forecastDayFocus=0;BuildPage();
  if(forecast.Count==0)RequestWeather();
  WriteState("weather_opened");
 }
 void OpenMetroDetail()
 {
  page=4;area=1;selected=0;editing="";lineDetailOpen=false;BuildPage();RequestLines();
  WriteState("metro_opened");
 }
 void ScrollForecastHours(string key)
 {
  int visible=Math.Min(12,forecastHours.Count);if(visible==0)return;
  if(key=="Left"){
   if(forecastHourFocus>0)forecastHourFocus--;
   else if(forecastHourOffset>0)forecastHourOffset--;
   else return;
  }else{
   if(forecastHourFocus+1<visible)forecastHourFocus++;
   else if(forecastHourOffset+visible<forecastHours.Count)forecastHourOffset++;
   else return;
  }
  BuildPage();WriteState("weather_hour_scroll");
 }
 void RefreshSettings(){
  currentVolume=sound_manager_get_master_volume(out var v)==0?v:-1;
  currentMute=sound_manager_get_master_mute(out var m)==0?m:-1;
  foreach(var id in new[]{"brightness","contrast","game-mode","sleeptimer","picturemode"}){
   try{if(CSharpGetItemValue(id,out var name,out var value)&&!string.IsNullOrEmpty(value)){settingValues[id]=value;settingNames[id]=name;}else{settingValues.Remove(id);settingNames.Remove(id);}
    settingEnabled[id]=CSharpGetItemEnable(id,out var enabled)&&enabled;
   }catch{settingValues.Remove(id);settingNames.Remove(id);settingEnabled[id]=false;}
  }
  foreach(var id in new[]{"brightness","contrast"}){try{double lo,hi,st;levelRange[id]=CSharpGetItemMinMaxStepValue(id,out lo,out hi,out st)&&hi>lo?new[]{lo,hi}:new[]{0d,50d};}catch{levelRange[id]=new[]{0d,50d};}}
 }
 string SettingValue(string id){return settingValues.TryGetValue(id,out var v)?v:"";}
 string SettingName(string id){return settingNames.TryGetValue(id,out var v)?v:"Indisponible";}
 bool CanEdit(string id){return settingEnabled.TryGetValue(id,out var enabled)&&enabled&&settingValues.ContainsKey(id);}
 void SetSetting(string id,string value){
  if(!CanEdit(id)){ShowError("Ce réglage est indisponible sur cette entrée.");return;}
  bool ok=false;try{ok=CSharpSetItemValue(id,value);}catch{}
  RefreshSettings();UpdateFocus();
  bool verified=ok&&SettingValue(id)==value;
  if(!verified)ShowError("La TV n’a pas appliqué le réglage. Essayez les paramètres TV.");
  WriteState("setting:"+id+":"+value+":"+verified);
 }
 // ---- picture mode: options come from the TV's own settings service as "first,second;first,second;" ----
 List<string[]> LoadPictureModes()
 {
  var raw=new List<string[]>();string text="";
  try{var sb=new System.Text.StringBuilder(1024);if(CSharpGetItemDataList("picturemode",sb))text=sb.ToString();}catch(Exception ex){text="error:"+ex.GetType().Name;}
  foreach(var part in text.Split(';')){int c=part.IndexOf(',');if(c>0)raw.Add(new[]{part.Substring(0,c).Trim(),part.Substring(c+1).Trim()});}
  string cur=SettingValue("picturemode"),curName=SettingName("picturemode");
  bool valueFirst=false;foreach(var r in raw)if(r[0]==cur&&r[1]!=cur||r[1]==curName&&r[0]!=curName){valueFirst=true;break;}
  // The service lists every input context (values share their high 16 bits per context); keep the current one, enabled, once per name.
  long curCode;long.TryParse(cur,out curCode);var list=new List<string[]>();var names=new HashSet<string>();
  foreach(var r in raw){
   var m=valueFirst?new[]{r[1],r[0]}:new[]{r[0],r[1]};long code;
   if(!long.TryParse(m[1],out code)||curCode>0&&code>>16!=curCode>>16||m[0].StartsWith("CAL-")||!names.Add(m[0]))continue;
   bool on=true;try{bool en;if(CSharpGetItemDataListEnable("picturemode",m[1],out en))on=en;}catch{}
   if(on)list.Add(m);
  }
  string[] order={"Dynamique","Standard","Naturel","Éco","Cinéma","FILMMAKER MODE","Loisirs","Jeux","Graphique"};
  list.Sort((x,y)=>{int ix=Array.IndexOf(order,x[0]),iy=Array.IndexOf(order,y[0]);return (ix<0?99:ix)-(iy<0?99:iy);});
  try{File.WriteAllText("/tmp/quiet-picture.txt","raw="+text+"\ncurrent_value="+cur+"\ncurrent_name="+curName+"\nvalue_first="+valueFirst+"\nenabled="+CanEdit("picturemode")+"\nshown="+string.Join("|",list.ConvertAll(m=>m[0]+"="+m[1]))+"\n");}catch{}
  return list;
 }
 void OpenPicture()
 {
  RefreshSettings();pictureModes=LoadPictureModes();
  if(pictureModes.Count==0){ShowError("Mode d’image indisponible sur cette entrée.");return;}
  pictureSel=0;string cur=SettingValue("picturemode");for(int i=0;i<pictureModes.Count;i++)if(pictureModes[i][1]==cur)pictureSel=i;
  pictureOpen=true;BuildPage();WriteState("picture_opened:"+pictureModes.Count);
 }
 void BuildPicturePopup()
 {
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.05f,.07f,.12f,.55f)});
  int n=Math.Min(pictureModes.Count,7),gap=16,pw=Math.Max(760,Math.Min(1700,n*236+(n-1)*gap+96)),ph=340,px=(1920-pw)/2,py=(1080-ph)/2;
  var panel=Panel(px+pw/2,py+ph/2,pw,ph,dark?new Color(.10f,.11f,.15f,.97f):new Color(.99f,.98f,.96f,.98f),44);panel.PivotPoint=new Position(.5f,.5f,.5f);panel.PositionUsesPivotPoint=true;panel.BorderlineWidth=1;panel.BorderlineColor=GlassEdge();panel.BoxShadow=new Shadow(80f,new Color(0,0,0,.35f),new Vector2(0,30));
  var t1=Label("Mode d’image",0,32,pw,44,32,ink,"Semibold");t1.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(t1);
  var t2=Label("Actuel : "+SettingName("picturemode"),0,80,pw,30,21,muted,"Medium");t2.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(t2);
  int bw=(pw-96-(n-1)*gap)/n,bh=112,sx=48,by=136;
  for(int i=0;i<n;i++){
   bool sel=i==pictureSel,cur=pictureModes[i][1]==SettingValue("picturemode");
   var b=new View{Position2D=new Position2D(sx+i*(bw+gap),by),Size2D=new Size2D(bw,bh),CornerRadius=new Vector4(18,18,18,18),BackgroundColor=sel?accent:Sf(.93f,.94f,.97f,1)};panel.Add(b);
   var bt=Label(pictureModes[i][0],10,cur?18:0,bw-20,cur?56:bh,pictureModes[i][0].Length>12?20:24,sel?white:ink,"Semibold");bt.HorizontalAlignment=HorizontalAlignment.Center;bt.MultiLine=true;bt.LineWrapMode=LineWrapMode.Word;b.Add(bt);
   if(cur){var c=Label("actuel",0,70,bw,26,16,sel?new Color(1,1,1,.85f):muted,"Medium");c.HorizontalAlignment=HorizontalAlignment.Center;b.Add(c);}
  }
  var t3=Label("←/→ choisir · OK appliquer · Retour annuler",0,ph-56,pw,28,19,muted,"Medium");t3.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(t3);
 }
 void HandlePictureKey(string k)
 {
  int n=Math.Min(pictureModes.Count,7);
  if(k=="Left"||k=="Right"){pictureSel=(pictureSel+(k=="Left"?-1:1)+n)%n;BuildPage();return;}
  if(k=="Return"||k=="KP_Enter"){var m=pictureModes[pictureSel];pictureOpen=false;BuildPage();SetSetting("picturemode",m[1]);if(notice.Text=="")notice.Text="Mode d’image : "+SettingName("picturemode");return;}
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){pictureOpen=false;BuildPage();}
 }
 // Brightness and contrast move in steps of 5, snapped to multiples of 5 and kept inside the TV's own range.
 void ChangeLevel(string id,int delta){double n;if(!double.TryParse(SettingValue(id),NumberStyles.Float,CultureInfo.InvariantCulture,out n))return;var r=levelRange.TryGetValue(id,out var v)?v:new[]{0d,50d};double next=(Math.Round(n/5)+delta)*5;SetSetting(id,Math.Max(r[0],Math.Min(r[1],next)).ToString(CultureInfo.InvariantCulture));}
 void ChangeVolume(int delta){int n;int r=sound_manager_get_master_volume(out n);if(r==0)r=sound_manager_set_master_volume(Math.Max(0,Math.Min(100,n+delta)));if(r!=0)ShowError("Utilisez les touches de volume de la télécommande.");RefreshSettings();UpdateFocus();WriteState("volume_changed");}
 void SelectSource(int source){int r=connect_source(source);if(r<0){ShowError("Impossible de changer d’entrée. Vérifiez la connexion dans les paramètres TV.");WriteState("source_failed:"+r);return;}lastSource=source;Launch("org.tizen.tv-viewer");}
 bool LaunchSystemApp(string id)
 {
  try{
   using(var process=Process.Start("/usr/bin/app_launcher","-u 5001 -s "+id+" launchstyle foreground")){
    if(process==null)return false;
    if(!process.WaitForExit(2000)||process.ExitCode==0){WriteState("launch:"+id+":app_launcher");return true;}
    WriteState("launch:"+id+":app_launcher:"+process.ExitCode);
   }
  }catch(Exception ex){WriteState("launch_error:"+id+":app_launcher:"+ex.GetType().Name);}
  return false;
 }
 // Everything that is due goes out as ONE helper launch; never while another app is in front (a reply would pull Home forward).
 void RequestDue()
 {
  if(isPaused)return;var now=DateTime.Now;
  bool home=page==0||ambientRoot!=null;
  if(home&&now-lastSensorRequest>=TimeSpan.FromMinutes(5)){lastSensorRequest=now;lastHaRequest=now;RequestHaStates();}
  bool w=(home||page==3)&&now-lastWeatherRequest>=TimeSpan.FromMinutes(forecast.Count==0?4:30);
  bool m=(home||page==4)&&now-lastMetroRequest>=TimeSpan.FromMinutes(1);
  bool t=(page==0||page==4)&&now-lastTrafficRequest>=TimeSpan.FromMinutes(5);
  bool n=page==0&&now-lastNewsRequest>=TimeSpan.FromMinutes(30);
  bool k=page==0&&now-lastMarketsRequest>=(quoteDetailOpen?TimeSpan.FromSeconds(60):TimeSpan.FromMinutes(newsMenuOpen&&newsCategoryIndex==3?2:5));
  bool np=(page==0||page==8)&&now-lastMediaRequest>=TimeSpan.FromSeconds(page==8?25:55);
  bool hubs=(page==8||briefRoot!=null||page==0&&prefWall==2)&&now-lastHubRequest>=TimeSpan.FromMinutes(plexItems.Count==0?1:10);
  bool fx=page==9&&now-lastFelixRequest>=TimeSpan.FromSeconds(110);
  bool mob=(page==0||page==10)&&now-lastMobilityRequest>=TimeSpan.FromSeconds(page==10?60:150);
  bool fi=page==11&&findUnlocked&&now-lastFindRequest>=TimeSpan.FromSeconds(60);
  if(np||hubs||fx||mob||fi)RequestExtra(np||hubs,hubs,fx,false,mob,fi);
  if(!(w||m||t||n||k))return;
  if(w)lastWeatherRequest=now;if(m)lastMetroRequest=now;if(t){lastTrafficRequest=now;lastLinesRequest=now;}if(n)lastNewsRequest=now;if(k)lastMarketsRequest=now;
  SendDataRequest(w,m,t,n,t,k);
 }
 // ---- theme: dark from 19:00 to 07:00 (or forced by /tmp/quiet-theme = dark|light) ----
 bool WantDark(){try{if(File.Exists("/tmp/quiet-theme")){string v=File.ReadAllText("/tmp/quiet-theme").Trim();if(v=="dark")return true;if(v=="light")return false;}}catch{}if(prefTheme=="dark")return true;if(prefTheme=="light")return false;var now=DateTime.Now;var sun=SunTimes(now.Date);return now<sun[0]||now>=sun[1];}
 // Light from sunrise to sunset in Paris (sun 3° below the horizon, close to civil twilight), computed locally with the NOAA approximation.
 static DateTime[] SunTimes(DateTime day)
 {
  const double lat=48.85,lon=2.35,zenith=93.0;double rad=Math.PI/180;
  int n=day.DayOfYear;var result=new DateTime[2];
  for(int k=0;k<2;k++){
   bool rise=k==0;double lngHour=lon/15,t=n+((rise?6:18)-lngHour)/24;
   double M=0.9856*t-3.289,L=M+1.916*Math.Sin(M*rad)+0.020*Math.Sin(2*M*rad)+282.634;L=(L%360+360)%360;
   double RA=Math.Atan(0.91764*Math.Tan(L*rad))/rad;RA=(RA%360+360)%360;RA=(RA+Math.Floor(L/90)*90-Math.Floor(RA/90)*90)/15;
   double sinDec=0.39782*Math.Sin(L*rad),cosDec=Math.Cos(Math.Asin(sinDec));
   double cosH=(Math.Cos(zenith*rad)-sinDec*Math.Sin(lat*rad))/(cosDec*Math.Cos(lat*rad));
   if(cosH>1||cosH<-1){result[k]=day.AddHours(rise?7:19);continue;}
   double H=(rise?360-Math.Acos(cosH)/rad:Math.Acos(cosH)/rad)/15,T=H+RA-0.06571*t-6.622,UT=((T-lngHour)%24+24)%24;
   result[k]=new DateTime(day.Year,day.Month,day.Day,0,0,0,DateTimeKind.Utc).AddHours(UT).ToLocalTime();
   if(result[k].Date!=day.Date)result[k]=day.Add(result[k].TimeOfDay);
  }
  return result;
 }
 void ApplyTheme()
 {
  dark=WantDark();
  ink=dark?new Color(.93f,.94f,.97f,1):inkFixed;muted=dark?new Color(.62f,.65f,.72f,1):new Color(0.431f,0.431f,0.451f,1);
  var a=(dark?accentDark:accentLight)[prefAccent];accent=new Color(a[0],a[1],a[2],1);
  Window.Default.BackgroundColor=dark?new Color(.05f,.06f,.09f,1):new Color(0.96f,0.96f,0.97f,1);
 }
 // ---- preferences kept in Home's data folder: theme, accent, wallpaper, morning brief ----
 string PrefsPath(){try{return System.IO.Path.Combine(Current.DirectoryInfo.Data,"prefs.txt");}catch{return "/tmp/quiet-prefs.txt";}}
 void LoadPrefs()
 {
  try{foreach(var line in File.ReadAllLines(PrefsPath())){
   int i=line.IndexOf('=');if(i<=0)continue;string key=line.Substring(0,i),v=line.Substring(i+1);int n;
   if(key=="theme"&&(v=="auto"||v=="light"||v=="dark"))prefTheme=v;
   else if(key=="accent"&&int.TryParse(v,out n))prefAccent=Math.Max(0,Math.Min(accentNames.Length-1,n));
   else if(key=="wall"&&int.TryParse(v,out n))prefWall=Math.Max(0,Math.Min(wallNames.Length-1,n));
   else if(key=="brief")prefBrief=v!="0";
   else if(key=="briefDate")briefDate=v;
  }}catch{}
 }
 void SavePrefs(){try{File.WriteAllText(PrefsPath(),"theme="+prefTheme+"\naccent="+prefAccent+"\nwall="+prefWall+"\nbrief="+(prefBrief?"1":"0")+"\nbriefDate="+briefDate+"\n");}catch{}}
 // ---- Apparence: a side sheet to pick the theme, the wallpaper (with previews) and the accent colour ----
 bool appearOpen;int appearRow,appearCol;
 static readonly string[] themeKeys={"auto","light","dark"},themeNames={"Automatique","Clair","Sombre"},themeHints={"Clair le jour, sombre la nuit","Toujours clair","Toujours sombre"};
 int AppearCurrent(int row){return row==0?Math.Max(0,Array.IndexOf(themeKeys,prefTheme)):row==1?prefWall:prefAccent;}
 int AppearCount(int row){return row==0?themeKeys.Length:row==1?wallNames.Length:accentNames.Length;}
 string WallPreview(int i)
 {
  if(i==0)return Root+"free-background.svg";
  int keep=prefWall;string f="";
  try{prefWall=i;f=i==1?WeatherWallFile():i==2?WallPoster():i==3?AuroraWallFile():SeventiesWallFile();}finally{prefWall=keep;}
  return f??"";
 }
 void BuildAppearance()
 {
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(0,0,0,dark?.45f:.25f)});
  int pw=1160,ph=1000,px=1920-40-pw,py=40;
  var sheet=Panel(px,py,pw,ph,dark?new Color(.10f,.11f,.15f,.96f):new Color(.99f,.98f,.96f,.97f),44);sheet.BorderlineWidth=1;sheet.BorderlineColor=GlassEdge();sheet.BoxShadow=new Shadow(80f,new Color(0,0,0,.35f),new Vector2(0,30));
  sheet.Add(Label("Apparence",56,44,800,56,44,ink,"Display"));
  sheet.Add(Label("Le choix s’applique tout de suite.",58,104,800,32,21,muted,"Medium"));
  int y=170;
  for(int row=0;row<3;row++){
   sheet.Add(Label(row==0?"THÈME":row==1?"FOND D’ÉCRAN":"COULEUR D’ACCENT",56,y,700,28,18,muted,"Semibold"));y+=40;
   int n=AppearCount(row),cur=AppearCurrent(row),gap=18,inner=pw-112;
   if(row==0){
    int cw=(inner-(n-1)*gap)/n,ch=96;
    for(int i=0;i<n;i++){bool f=appearRow==row&&appearCol==i,act=cur==i;
     var c=new View{Position2D=new Position2D(56+i*(cw+gap),y),Size2D=new Size2D(cw,ch),CornerRadius=new Vector4(24,24,24,24),BackgroundColor=act?accent:Sf(.93f,.92f,.90f,1),BorderlineWidth=f?4:0,BorderlineColor=act?ink:accent};sheet.Add(c);
     c.Add(Label(themeNames[i],22,12,cw-44,38,26,act?white:ink,"Semibold"));c.Add(Label(themeHints[i],22,52,cw-44,28,19,act?new Color(1,1,1,.85f):muted,"Medium"));}
    y+=96+40;
   }else if(row==1){
    int cw=(inner-(n-1)*gap)/n,ch=cw*9/16;
    for(int i=0;i<n;i++){bool f=appearRow==row&&appearCol==i,act=cur==i;
     var c=new View{Position2D=new Position2D(56+i*(cw+gap),y),Size2D=new Size2D(cw,ch),CornerRadius=new Vector4(18,18,18,18),BackgroundColor=Sf(.85f,.85f,.88f,1),BorderlineWidth=f?5:act?3:0,BorderlineColor=f?accent:ink,ClippingMode=ClippingModeType.ClipChildren};sheet.Add(c);
     string img=WallPreview(i);
     if(img!="")c.Add(new ImageView{ResourceUrl=img,Size2D=new Size2D(cw,ch),FittingMode=FittingModeType.ScaleToFill,DesiredWidth=cw*2,DesiredHeight=ch*2});
     else{var ph2=Label("Ouvre Plex",0,0,cw,ch,18,muted,"Medium");ph2.HorizontalAlignment=HorizontalAlignment.Center;c.Add(ph2);}
     if(i==0&&dark)c.Add(new View{Size2D=new Size2D(cw,ch),BackgroundColor=new Color(.04f,.05f,.08f,.85f)});
     var nl=Label((act?"✓ ":"")+wallNames[i],56+i*(cw+gap),y+ch+8,cw,32,21,act?ink:muted,act?"Semibold":"Medium");nl.HorizontalAlignment=HorizontalAlignment.Center;sheet.Add(nl);}
    y+=(inner-(n-1)*gap)/n*9/16+60;
   }else{
    int cw=(inner-(n-1)*gap)/n;
    for(int i=0;i<n;i++){bool f=appearRow==row&&appearCol==i,act=cur==i;var a=(dark?accentDark:accentLight)[i];
     var sw=new View{Position2D=new Position2D(56+i*(cw+gap)+(cw-72)/2,y),Size2D=new Size2D(72,72),CornerRadius=new Vector4(36,36,36,36),BackgroundColor=new Color(a[0],a[1],a[2],1),BorderlineWidth=f?5:act?3:0,BorderlineColor=f?ink:Sf(1,1,1,1)};sheet.Add(sw);
     var nl=Label(accentNames[i],56+i*(cw+gap),y+80,cw,30,20,act?ink:muted,act?"Semibold":"Medium");nl.HorizontalAlignment=HorizontalAlignment.Center;sheet.Add(nl);}
    y+=72+60;
   }
  }
  var hint=Label("↑/↓ rubrique · ←/→ choisir · OK appliquer · Retour fermer",0,ph-58,pw,30,20,muted,"Medium");hint.HorizontalAlignment=HorizontalAlignment.Center;sheet.Add(hint);
 }
 void HandleAppearanceKey(string k)
 {
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){appearOpen=false;BuildPage();return;}
  if(k=="Up"||k=="Down"){appearRow=Math.Max(0,Math.Min(2,appearRow+(k=="Up"?-1:1)));appearCol=Math.Min(AppearCurrent(appearRow),AppearCount(appearRow)-1);BuildPage();return;}
  if(k=="Left"||k=="Right"){appearCol=Math.Max(0,Math.Min(AppearCount(appearRow)-1,appearCol+(k=="Left"?-1:1)));BuildPage();return;}
  if(k!="Return"&&k!="KP_Enter")return;
  if(appearRow==0)prefTheme=themeKeys[appearCol];
  else if(appearRow==1){prefWall=appearCol;if(prefWall==2&&plexItems.Count==0)RequestExtra(true,true,false);}
  else prefAccent=appearCol;
  SavePrefs();ApplyTheme();BuildPage();WriteState("appearance:"+appearRow+":"+appearCol);
 }
 void CyclePref(string key)
 {
  if(key=="theme")prefTheme=prefTheme=="auto"?"light":prefTheme=="light"?"dark":"auto";
  else if(key=="accent")prefAccent=(prefAccent+1)%accentNames.Length;
  else if(key=="wall"){prefWall=(prefWall+1)%wallNames.Length;if(prefWall==2&&plexItems.Count==0)RequestExtra(true,true,false);}
  else if(key=="brief")prefBrief=!prefBrief;
  SavePrefs();ApplyTheme();BuildPage();
  notice.Text=key=="wall"&&prefWall==2&&WallPoster()==""?"Affiche en chargement depuis Plex…":"";
  WriteState("pref:"+key);
 }
 void AppearanceTile(string name,string action,string description,int column)
 {
  int w=276,x=80+column*(w+20),y=878,h=108;
  var card=Panel(x+w/2,y+h/2,w,h,GlassFill(),22);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
  card.Add(Label(name.ToUpperInvariant(),22,14,w-40,24,14,muted,"Semibold"));
  var value=Label("",22,40,w-100,52,26,ink,"Semibold");card.Add(value);
  var swatch=new View{Position2D=new Position2D(w-80,(h-52)/2),Size2D=new Size2D(52,52),CornerRadius=new Vector4(26,26,26,26)};card.Add(swatch);
  if(action=="pref:accent")swatch.BackgroundColor=accent;
  else if(action=="app:org.tizen.menu"){swatch.BackgroundColor=Sf(.92f,.93f,.96f,1);swatch.Add(new ImageView{ResourceUrl=Root+"symbol-tv_settings.png",Color=ink,Position2D=new Position2D(11,11),Size2D=new Size2D(30,30)});}
  else if(action=="controllers"){swatch.BackgroundColor=Sf(.92f,.93f,.96f,1);swatch.Add(new ImageView{ResourceUrl=Root+"symbol-gamepad.png",Color=ink,Position2D=new Position2D(11,11),Size2D=new Size2D(30,30)});}
  else if(action=="pref:theme"){swatch.BackgroundColor=dark?new Color(.12f,.14f,.2f,1):new Color(1,1,1,1);swatch.BorderlineWidth=2;swatch.BorderlineColor=Tx(.8f,.82f,.86f,1);swatch.Add(new View{Position2D=new Position2D(26,0),Size2D=new Size2D(26,52),BackgroundColor=dark?new Color(1,1,1,1):new Color(.12f,.14f,.2f,1),CornerRadius=new Vector4(0,26,26,0)});}
  else if(action=="pref:wall"){string f=WallpaperPreview();if(f!="")swatch.Add(new ImageView{ResourceUrl=f,Size2D=new Size2D(52,52),FittingMode=FittingModeType.ScaleToFill,DesiredWidth=52,DesiredHeight=52,CornerRadius=new Vector4(26,26,26,26)});else swatch.BackgroundColor=Sf(.9f,.91f,.94f,1);}
  else{swatch.Size2D=new Size2D(76,42);swatch.Position2D=new Position2D(w-104,(h-42)/2);swatch.CornerRadius=new Vector4(21,21,21,21);swatch.BackgroundColor=prefBrief?accent:Sf(.84f,.85f,.88f,1);swatch.Add(new View{Position2D=new Position2D(prefBrief?38:4,4),Size2D=new Size2D(34,34),BackgroundColor=white,CornerRadius=new Vector4(17,17,17,17)});}
  slots.Add(new Slot{Card=card,Label=value,Name=name,Action=action,Description=description,Row=2,X=x,Y=y,W=w,H=h});
 }
 // ---- wallpapers: classic artwork, a sky that follows the weather and the hour, a blurred Plex poster, or an accent aurora ----
 static string Hex(float r,float g,float b){return "#"+((int)(Math.Max(0,Math.Min(1,r))*255)).ToString("X2")+((int)(Math.Max(0,Math.Min(1,g))*255)).ToString("X2")+((int)(Math.Max(0,Math.Min(1,b))*255)).ToString("X2");}
 string WallKind()
 {
  int c=weatherCode;bool night=DateTime.Now.Hour>=19||DateTime.Now.Hour<7;
  string sky=c<0?"cloud":c<=1?"clear":c<=3?"cloud":c==45||c==48?"fog":(c>=71&&c<=77)||c==85||c==86?"snow":c>=51?"rain":"cloud";
  return sky+(night?"-night":"-day")+(dark?"-d":"-l");
 }
 string WallFile(string key,string svg)
 {
  try{
   string dir=System.IO.Path.Combine(Current.DirectoryInfo.Data,"wall");Directory.CreateDirectory(dir);
   string file=System.IO.Path.Combine(dir,key+".svg");if(!File.Exists(file))File.WriteAllText(file,svg);
   return file;
  }catch{return "";}
 }
 string WeatherWallFile()
 {
  string kind=WallKind();var p=kind.Split('-');bool night=p[1]=="night",d=dark;
  string top,bottom,glow;double gx=.84,gy=.06,go=.85;
  switch(p[0]){
   case "clear":top=d?(night?"#0B1734":"#10264A"):"#C9DFFB";bottom=d?"#090C14":"#F6F8FC";glow=d?(night?"#3A5FA8":"#E8B860"):"#FFE2A0";break;
   case "rain":top=d?"#101B2B":"#C8D4E2";bottom=d?"#080B11":"#EEF1F5";glow=d?"#24476C":"#A8C1DC";go=.7;break;
   case "fog":top=d?"#171A20":"#E2E5E9";bottom=d?"#0B0C0F":"#F6F6F7";glow=d?"#2C3038":"#FFFFFF";gx=.5;gy=.0;go=.6;break;
   case "snow":top=d?"#152238":"#DDE9F6";bottom=d?"#0A0E16":"#FAFCFE";glow=d?"#3C5274":"#FFFFFF";break;
   default:top=d?"#141B26":"#D9E1EB";bottom=d?"#0A0D13":"#F3F5F8";glow=d?"#2A3646":"#FFFFFF";go=.75;break;
  }
  if(!d&&night){top="#D3D9EA";glow="#B7C3E6";}
  var inv=CultureInfo.InvariantCulture;
  string svg="<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1920\" height=\"1080\" viewBox=\"0 0 1920 1080\"><defs>"
   +"<linearGradient id=\"s\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\""+top+"\"/><stop offset=\"1\" stop-color=\""+bottom+"\"/></linearGradient>"
   +"<radialGradient id=\"g\" cx=\""+gx.ToString(inv)+"\" cy=\""+gy.ToString(inv)+"\" r=\".62\"><stop offset=\"0\" stop-color=\""+glow+"\" stop-opacity=\""+go.ToString(inv)+"\"/><stop offset=\"1\" stop-color=\""+glow+"\" stop-opacity=\"0\"/></radialGradient>"
   +"</defs><rect width=\"1920\" height=\"1080\" fill=\"url(#s)\"/><rect width=\"1920\" height=\"1080\" fill=\"url(#g)\"/></svg>";
  wallKind=kind;
  return WallFile("sky-"+kind,svg);
 }
 // "Années 70": a striped sunset rising behind the top of the screen (rust, orange, mustard, brown) over warm cream;
 // the lower half stays plain so the rows read well, and the hero backdrop fades into the same cream.
 string SeventiesBg(){return dark?"#1B120D":"#F3E6D1";}
 string SeventiesWallFile()
 {
  string bg=SeventiesBg();string op=dark?".62":"1";
  string[] col={"#7A3E1D","#C4512A","#E8792C","#F2A93B","#F6CF6B"};int[] rad={496,416,336,256,176};
  var sb=new System.Text.StringBuilder();
  sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1920\" height=\"1080\" viewBox=\"0 0 1920 1080\"><defs><clipPath id=\"h\"><rect width=\"1920\" height=\"612\"/></clipPath></defs>");
  sb.Append("<rect width=\"1920\" height=\"1080\" fill=\""+bg+"\"/>");
  sb.Append("<g clip-path=\"url(#h)\" opacity=\""+op+"\">");
  for(int i=0;i<col.Length;i++)sb.Append("<circle cx=\"1540\" cy=\"612\" r=\""+rad[i]+"\" fill=\""+col[i]+"\"/>");
  int[] sy={566,532,500,470,442,416},sh={14,12,10,8,6,4};
  for(int i=0;i<sy.Length;i++)sb.Append("<rect x=\"900\" y=\""+sy[i]+"\" width=\"1020\" height=\""+sh[i]+"\" fill=\""+bg+"\"/>");
  sb.Append("</g>");
  sb.Append("</svg>");
  return WallFile("seventies3"+(dark?"-d":"-l"),sb.ToString());
 }
 string AuroraWallFile()
 {
  var a=(dark?accentDark:accentLight)[prefAccent];
  string c1=Hex(a[0],a[1],a[2]),c2=Hex(a[2]*.9f+.05f,a[0]*.6f+.2f,a[1]*.8f+.15f),c3=Hex(a[1]*.7f+.25f,a[2]*.5f+.25f,a[0]*.9f);
  string bg=dark?"#090B12":"#F4F5F8";string op=dark?".42":".30";
  string svg="<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1920\" height=\"1080\" viewBox=\"0 0 1920 1080\"><defs>"
   +"<radialGradient id=\"a\" cx=\".18\" cy=\".12\" r=\".55\"><stop offset=\"0\" stop-color=\""+c1+"\" stop-opacity=\""+op+"\"/><stop offset=\"1\" stop-color=\""+c1+"\" stop-opacity=\"0\"/></radialGradient>"
   +"<radialGradient id=\"b\" cx=\".88\" cy=\".28\" r=\".5\"><stop offset=\"0\" stop-color=\""+c2+"\" stop-opacity=\""+op+"\"/><stop offset=\"1\" stop-color=\""+c2+"\" stop-opacity=\"0\"/></radialGradient>"
   +"<radialGradient id=\"c\" cx=\".55\" cy=\"1\" r=\".6\"><stop offset=\"0\" stop-color=\""+c3+"\" stop-opacity=\""+op+"\"/><stop offset=\"1\" stop-color=\""+c3+"\" stop-opacity=\"0\"/></radialGradient>"
   +"</defs><rect width=\"1920\" height=\"1080\" fill=\""+bg+"\"/><rect width=\"1920\" height=\"1080\" fill=\"url(#a)\"/><rect width=\"1920\" height=\"1080\" fill=\"url(#b)\"/><rect width=\"1920\" height=\"1080\" fill=\"url(#c)\"/></svg>";
  return WallFile("aurora-"+prefAccent+(dark?"-d":"-l"),svg);
 }
 string WallPoster()
 {
  if(nowPlaying!=null&&nowPlaying.Image!=""&&File.Exists(nowPlaying.Image))return nowPlaying.Image;
  foreach(var item in plexItems)if(item.Image!=""&&File.Exists(item.Image))return item.Image;
  return "";
 }
 string WallpaperPreview(){return prefWall==1?WeatherWallFile():prefWall==2?WallPoster():prefWall==3?AuroraWallFile():Root+"free-background.svg";}
 void AddWallpaper()
 {
  string poster=prefWall==2?WallPoster():"";
  if(poster!=""){
   Add(new ImageView{ResourceUrl=poster,Size2D=new Size2D(1920,1080),FittingMode=FittingModeType.ScaleToFill,DesiredWidth=48,DesiredHeight=27});
   Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=dark?new Color(.03f,.04f,.07f,.78f):new Color(.97f,.97f,.98f,.80f)});
   return;
  }
  string file=prefWall==1?WeatherWallFile():prefWall==3?AuroraWallFile():prefWall==4?SeventiesWallFile():"";
  if(file!=""){
   Add(new ImageView{ResourceUrl=file,Size2D=new Size2D(1920,1080)});
   // inner pages: the artwork stays as a ghost so cards and text read cleanly over it
   if(page!=0||homeShelf==1)Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=prefWall==4?(dark?new Color(.106f,.071f,.051f,.72f):new Color(.953f,.902f,.82f,.78f)):(dark?new Color(.03f,.04f,.07f,.55f):new Color(.97f,.97f,.98f,.6f))});
   return;}
  Add(new ImageView{ResourceUrl=Root+"free-background.svg",Size2D=new Size2D(1920,1080)});
  if(dark)Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.04f,.05f,.08f,.93f)});
 }
 // Light surface / grey text literal -> its dark-theme counterpart.
 Color Sf(float r,float g,float b,float a){if(!dark)return new Color(r,g,b,a);float v=.15f+(1f-(r+g+b)/3f)*1.1f;return new Color(v,v+.012f,v+.045f,a<.9f?Math.Min(1f,a+.06f):a);}
 Color Tx(float r,float g,float b,float a){if(!dark)return new Color(r,g,b,a);float v=1f-(r+g+b)/3f*.6f;return new Color(v,v,Math.Min(1f,v+.03f),a);}
 // ---- apps ordered by how often they are launched ----
 int Use(string id){return appUse.TryGetValue(id,out var n)?n:0;}
 string UsagePath(){try{return System.IO.Path.Combine(Current.DirectoryInfo.Data,"app-usage.txt");}catch{return "/tmp/quiet-app-usage.txt";}}
 void SortLibrary(){library.Sort((x,y)=>{int ux=Use(x.Id),uy=Use(y.Id);return ux!=uy?uy-ux:appOrder[x.Id]-appOrder[y.Id];});}
 void LoadUsage()
 {
  for(int i=0;i<library.Count;i++)appOrder[library[i].Id]=i;
  try{foreach(var line in File.ReadAllLines(UsagePath())){int t=line.IndexOf('\t');int n;if(t>0&&int.TryParse(line.Substring(t+1),out n)&&n>0)appUse[line.Substring(0,t)]=n;}}catch{}
  SortLibrary();
 }
 void BumpUsage(string id)
 {
  appUse[id]=Use(id)+1;usageDirty=true;if(appOrder.ContainsKey(id))SortLibrary();
  try{var sb=new System.Text.StringBuilder();foreach(var kv in appUse)sb.Append(kv.Key).Append('\t').Append(kv.Value).Append('\n');File.WriteAllText(UsagePath(),sb.ToString());}catch{}
 }
 // ---- home strip: room temperatures and last Linky reading from Home Assistant ----
 string SensorNum(string id,string format){string v;double d;return haSensor.TryGetValue(id,out v)&&double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out d)?d.ToString(format,CultureInfo.InvariantCulture).Replace('.',','):"--";}
 void BuildHomeSensors()
 {
  int x=InfoX(4),y=infoY,w=infoW,h=infoH;
  var card=InfoTile(4);
  card.Add(Label("MAISON",22,14,w-44,28,18,muted,"Semibold"));
  string[] cap={"Salon","Chambre","Électricité"};homeSensorValue=new TextLabel[3];
  for(int i=0;i<3;i++){card.Add(Label(cap[i],22,50+i*42,120,36,20,muted,"Medium"));homeSensorValue[i]=Label("",130,50+i*42,w-152,36,21,ink,"Semibold");homeSensorValue[i].HorizontalAlignment=HorizontalAlignment.End;card.Add(homeSensorValue[i]);}
  slots.Add(new Slot{Card=card,Name="Maison",Action="page:5",Description="Température, humidité, électricité · OK pour la page Maison",Row=-2,X=x,Y=y,W=w,H=h});
  UpdateHomeSensors();
 }
 void UpdateHomeSensors()
 {
  if(homeSensorValue==null)return;
  homeSensorValue[0].Text=SensorNum("sensor.salon_sensor_temperature","0.0")+"° · "+SensorNum("sensor.salon_sensor_humidite","0")+" %";
  homeSensorValue[1].Text=SensorNum("sensor.chambre_temperature_temperature","0.0")+"° · "+SensorNum("sensor.chambre_temperature_humidite","0")+" %";
  string linky="--";foreach(var kv in haSensor)if(kv.Key.StartsWith("sensor.linky_")&&kv.Key.EndsWith("_dernier_releve"))linky=SensorNum(kv.Key,"0.0");
  homeSensorValue[2].Text=linky+" kWh";
 }
 // ---- ambient mode: dim drifting clock after 5 minutes without input (protects the OLED panel) ----
 int AmbientDelay(){try{int n;if(File.Exists("/tmp/quiet-ambient")&&int.TryParse(File.ReadAllText("/tmp/quiet-ambient").Trim(),out n)&&n>=10)return n;}catch{}return 300;}
 string ShortNext(string direction){var d=NextMetro(direction);return d==null?"—":d.AtStop?"à quai":d.Minutes==0?"<1 min":d.Minutes+" min";}
 void OpenAmbient()
 {
  if(ambientRoot!=null)return;
  ambientRoot=new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(0,0,0,1)};
  ambientBlock=new View{Position2D=new Position2D(140,300),Size2D=new Size2D(1000,490)};ambientRoot.Add(ambientBlock);
  var soft=new Color(.80f,.83f,.90f,1);var dim=new Color(.50f,.54f,.62f,1);
  ambientClock=Label("",0,0,1000,250,200,soft,"Display");ambientBlock.Add(ambientClock);
  ambientDate=Label("",8,262,1000,48,34,dim,"Medium");ambientBlock.Add(ambientDate);
  ambientInfo=Label("",8,324,1000,44,30,soft,"Medium");ambientBlock.Add(ambientInfo);
  ambientIndoor=Label("",8,376,1000,44,30,soft,"Medium");ambientBlock.Add(ambientIndoor);
  ambientMetro=Label("",8,432,1000,40,26,dim,"Medium");ambientBlock.Add(ambientMetro);
  Window.Default.Add(ambientRoot);ambientRoot.RaiseToTop();ambientStep=0;UpdateAmbient();
  ambientTimer=new Tizen.NUI.Timer(20000);ambientTimer.Tick+=(o,a)=>{if(ambientRoot==null)return false;ambientStep++;UpdateAmbient();return true;};ambientTimer.Start();
  RequestDue();WriteState("ambient_opened");
 }
 void UpdateAmbient()
 {
  if(ambientRoot==null)return;var now=DateTime.Now;
  ambientClock.Text=now.ToString("HH:mm");
  ambientDate.Text=frDays[(int)now.DayOfWeek]+" "+now.Day+" "+frMonths[now.Month-1];
  ambientInfo.Text=forecast.Count>0?"Paris 12e    "+weatherTemperatureText+"    "+weatherConditionText:"";
  string ts=SensorNum("sensor.salon_sensor_temperature","0.0"),tc=SensorNum("sensor.chambre_temperature_temperature","0.0");
  ambientIndoor.Text=ts=="--"&&tc=="--"?"":"Salon "+ts+"°    Chambre "+tc+"°";
  ambientMetro.Text=metroAvailable?"Métro 6    Nation "+ShortNext("Nation")+"    ·    Étoile "+ShortNext("Etoile"):"";
  if(ambientStep>0&&ambientStep%3==0){var rnd=new Random();var move=new Animation(6000);move.AnimateTo(ambientBlock,"Position",new Position(80+rnd.Next(0,680),80+rnd.Next(0,350),0));move.Play();}
 }
 void CloseAmbient()
 {
  if(ambientRoot==null)return;
  if(ambientTimer!=null){ambientTimer.Stop();ambientTimer.Dispose();ambientTimer=null;}
  Window.Default.Remove(ambientRoot);ambientRoot.Dispose();ambientRoot=null;lastInput=DateTime.Now;WriteState("ambient_closed");
 }
 // ---- Maison page: automatic cinema mode switch and Félix the robot vacuum ----
 string SensorState(string id){return haSensor.TryGetValue(id,out var v)?v:"";}
 void BuildDevices(int bx,int lw,int lg)
 {
  int cell=haLampId.Length,x=bx+(cell%5)*(lw+lg),y=mLtY+(cell/5)*(mLtH+mLtG);
  cinemaCard=Panel(x+lw/2,y+mLtH/2,lw,mLtH,GlassFill(),28);cinemaCard.PivotPoint=new Position(.5f,.5f,.5f);cinemaCard.PositionUsesPivotPoint=true;cinemaCard.BorderlineWidth=1;cinemaCard.BorderlineColor=GlassEdge();
  cinemaDot=new View{Position2D=new Position2D(22,(mLtH-60)/2),Size2D=new Size2D(60,60),CornerRadius=new Vector4(30,30,30,30)};cinemaCard.Add(cinemaDot);
  cinemaDot.Add(new ImageView{ResourceUrl=IconFile("film","#FFFFFF"),Position2D=new Position2D(15,15),Size2D=new Size2D(30,30)});
  cinemaCard.Add(Label("Cinéma auto",98,22,lw-110,34,24,ink,"Semibold"));
  cinemaState=Label("",98,58,lw-110,28,19,muted,"Medium");cinemaCard.Add(cinemaState);
  slots.Add(new Slot{Card=cinemaCard,Name="Mode cinéma automatique",Action="ha_auto:"+cinemaAuto,Description="Plex ou Netflix sur l’Apple TV lancent Film ou Pause · OK pour activer ou couper",Row=1+cell/5,X=x,Y=y,W=lw,H=mLtH});
  UpdateDevices();
 }
 static string FelixText(string state)
 {
  switch(state){
   case "charging":return "En charge";case "docked":return "Sur sa base";case "cleaning":return "Nettoyage en cours";
   case "returning":case "returning_to_dock":return "Retour à la base";case "paused":return "En pause";case "idle":return "À l’arrêt";
   case "error":return "Erreur";case "washing":case "washing_mop":case "mop_washing":case "washing_the_mop":return "Lavage de la serpillière";case "drying":return "Séchage";
   case "segment_cleaning":return "Nettoyage des pièces";case "zoned_cleaning":return "Nettoyage de zone";case "spot_cleaning":return "Nettoyage localisé";
   case "going_to_wash_the_mop":return "Va laver la serpillière";case "emptying_the_bin":return "Vidage du bac";case "returning_home":return "Retour à la base";
   case "charging_complete":return "Chargé";case "going_to_target":return "En route";case "mapping":return "Cartographie";case "sleeping":return "En veille";case "starting":return "Démarrage";
   case "":return "État inconnu";default:return state.Replace('_',' ');
  }
 }
 void UpdateDevices()
 {
  if(cinemaState!=null){
   string st=SensorState(cinemaAuto);bool on=st=="on";
   cinemaState.Text=st==""?"…":on?"Activé":"Coupé";cinemaState.TextColor=on?accent:muted;
   cinemaDot.BackgroundColor=on?accent:Sf(.80f,.79f,.77f,1);cinemaCard.BackgroundColor=on?new Color(accent.R,accent.G,accent.B,dark?.22f:.16f):GlassFill();
  }
  if(felixState!=null){
   string st=SensorState("sensor.felix_etat");if(st==""||st=="unknown")st=SensorState("vacuum.felix");
   string battery=SensorState("sensor.felix_batterie");
   felixState.Text=FelixText(st)+(battery!=""&&battery!="unknown"?" · "+battery+" %":"");
  }
 }
 // ---- Énergie page: daily electricity over the last 30 days ----
 void RequestEnergy(){if(isPaused)return;lastEnergyRequest=DateTime.Now;SendDataRequest(false,false,false,false,false,false,true);}
 void ReceiveEnergy(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){
   var list=new List<EnergyDay>();
   foreach(var el in doc.RootElement.EnumerateArray())list.Add(new EnergyDay{Date=el.GetProperty("Date").GetString()??"",Kwh=el.GetProperty("Kwh").GetDouble(),Eur=el.GetProperty("Eur").GetDouble()});
   if(list.Count>0){energyDays.Clear();energyDays.AddRange(list);}
  }}catch{}
  if(page==7){int a=area,sel=selected;BuildPage();area=a;selected=Math.Min(sel,slots.Count-1);UpdateFocus(false);}
  WriteState("energy_received:"+energyDays.Count);
 }
 string Kwh(double v){return v.ToString(v>=100?"0":"0.0",frCulture)+" kWh";}
 string Eur(double v){return v.ToString("0.00",frCulture)+" €";}
 void BuildEnergy()
 {
  if(energyDays.Count==0||(DateTime.Now-lastEnergyRequest).TotalMinutes>=30){if((DateTime.Now-lastEnergyRequest).TotalSeconds>=20)RequestEnergy();}
  if(energyDays.Count==0){Text("Chargement des relevés Linky…",80,300,1200,50,30,muted,"Medium");return;}
  double total=0,cost=0,max=0,month=0,monthCost=0;foreach(var d in energyDays){total+=d.Kwh;cost+=d.Eur;if(d.Kwh>max)max=d.Kwh;if(d.Date.StartsWith(DateTime.Today.ToString("yyyy-MM")) ){month+=d.Kwh;monthCost+=d.Eur;}}
  double avg=total/energyDays.Count;var last=energyDays[energyDays.Count-1];
  DateTime lastDate;DateTime.TryParse(last.Date,CultureInfo.InvariantCulture,DateTimeStyles.None,out lastDate);
  double vsAvg=avg>0?(last.Kwh-avg)/avg*100:0;
  string[] cap={energyDays.Count+" derniers jours","Moyenne par jour","Dernier relevé · "+frDays[(int)lastDate.DayOfWeek].Substring(0,3)+". "+lastDate.Day+"/"+lastDate.Month,"Ce mois-ci · "+frMonths[DateTime.Today.Month-1]};
  string[] big={Kwh(total),Kwh(avg),Kwh(last.Kwh),Kwh(month)};
  string[] small={"≈ "+Eur(cost),"≈ "+Eur(cost/energyDays.Count)+" par jour",Eur(last.Eur)+" · "+(vsAvg>=0?"+":"−")+Math.Abs(vsAvg).ToString("0",frCulture)+" % vs moyenne","≈ "+Eur(monthCost)};
  for(int i=0;i<4;i++){
   var card=Panel(80+i*446,232,422,136,GlassFill(),22);
   card.Add(Label(cap[i].ToUpperInvariant(),24,16,380,24,15,muted,"Semibold"));
   card.Add(Label(big[i],24,42,380,50,40,i==2?accent:ink,"Display"));
   card.Add(Label(small[i],24,96,380,26,17,muted,"Medium"));
  }
  var chart=Panel(80,392,1760,600,GlassFill(),24);
  chart.Add(Label("Consommation quotidienne",32,20,800,34,24,ink,"Semibold"));
  var legend=Label("Moyenne "+Kwh(avg)+" · samedi et dimanche plus clairs",900,22,828,30,17,muted,"Medium");legend.HorizontalAlignment=HorizontalAlignment.End;chart.Add(legend);
  int left=40,right=1720,top=104,bottom=516,n=energyDays.Count;double scale=max>0?(bottom-top-30)/max:0;
  float pitch=(right-left)/(float)n;int bw=(int)(pitch*.62f);
  int ay=bottom-(int)(avg*scale);
  chart.Add(new View{Position2D=new Position2D(left,ay),Size2D=new Size2D(right-left,2),BackgroundColor=new Color(accent.R,accent.G,accent.B,.45f)});
  chart.Add(new View{Position2D=new Position2D(left,bottom),Size2D=new Size2D(right-left,2),BackgroundColor=Sf(.88f,.9f,.94f,1)});
  for(int i=0;i<n;i++){
   var d=energyDays[i];DateTime date;DateTime.TryParse(d.Date,CultureInfo.InvariantCulture,DateTimeStyles.None,out date);
   bool weekend=date.DayOfWeek==DayOfWeek.Saturday||date.DayOfWeek==DayOfWeek.Sunday,latest=i==n-1;
   int h=Math.Max(4,(int)(d.Kwh*scale)),x=left+(int)(i*pitch+(pitch-bw)/2);
   float alpha=latest?1f:weekend?.45f:.78f;
   chart.Add(new View{Position2D=new Position2D(x,bottom-h),Size2D=new Size2D(bw,h),BackgroundColor=new Color(accent.R,accent.G,accent.B,alpha),CornerRadius=new Vector4(6,6,0,0)});
   var v=Label(d.Kwh.ToString("0.0",frCulture),x-12,bottom-h-26,bw+24,22,14,latest?ink:muted,latest?"Semibold":"Medium");v.HorizontalAlignment=HorizontalAlignment.Center;chart.Add(v);
   string dayLabel=(date.Day==1||i==0?frMonths[date.Month-1].Substring(0,3)+". ":"")+date.Day;
   var dl=Label(dayLabel,x-30,bottom+10,bw+60,22,14,weekend?muted:ink,"Medium");dl.HorizontalAlignment=HorizontalAlignment.Center;chart.Add(dl);
  }
  chart.Add(Label("Relevés Linky via Octopus Energy · publiés avec un jour de décalage · OK pour actualiser",32,556,1700,28,16,muted,"Medium"));
 }
 // ---- media: what plays on the Apple TV (home hero) and Plex "Reprendre" (Plex page) ----
 void RequestExtra(bool media,bool hubs,bool felixData,bool live=false,bool mobility=false,bool find=false)
 {
  if(isPaused)return;var now=DateTime.Now;if(mobility)lastMobilityRequest=now;if(find)lastFindRequest=now;
  if(media)lastMediaRequest=now;if(hubs)lastHubRequest=now;if(felixData)lastFelixRequest=now;
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0&&(media||hubs))r=app_control_add_extra_data(c,"quiet_media_request",hubs?"all":"now");
   if(r==0&&felixData)r=app_control_add_extra_data(c,"quiet_felix_request",live?"2":"1");
   if(r==0&&mobility)r=app_control_add_extra_data(c,"quiet_mobility_request","1");
   if(r==0&&find)r=app_control_add_extra_data(c,"quiet_find_request","1");
   if(r==0&&DateTime.Now-lastFactsRequest>=TimeSpan.FromHours(6)){lastFactsRequest=DateTime.Now;r=app_control_add_extra_data(c,"quiet_facts_request","1");}
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
 }
 static string JStr(JsonElement el,string name){return el.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";}
 static double JNum(JsonElement el,string name){return el.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.Number?v.GetDouble():0;}
 void ReceiveMedia(string json)
 {
  bool itemsChanged=false,nowChanged=false;
  try{using(var doc=JsonDocument.Parse(json)){
   var root=doc.RootElement;
   if(root.TryGetProperty("Now",out var n)){
    NowItem next=null;
    if(n.ValueKind==JsonValueKind.Object)next=new NowItem{Source=JStr(n,"Source"),App=JStr(n,"App"),Title=JStr(n,"Title"),Sub=JStr(n,"Sub"),State=JStr(n,"State"),Image=JStr(n,"Image"),Updated=JStr(n,"Updated"),Pos=JNum(n,"Pos"),Dur=JNum(n,"Dur")};
    nowChanged=(next==null)!=(nowPlaying==null)||next!=null&&(next.Title!=nowPlaying.Title||next.Sub!=nowPlaying.Sub||next.State!=nowPlaying.State||next.Image!=nowPlaying.Image);
    nowPlaying=next;
   }
   if(root.TryGetProperty("Items",out var noItems)&&noItems.ValueKind==JsonValueKind.Null&&!mediaLoaded){plexOffline=true;itemsChanged=true;}
   if(root.TryGetProperty("Items",out var items)&&items.ValueKind==JsonValueKind.Array){plexOffline=false;
    var list=new List<PlexItem>();
    foreach(var it in items.EnumerateArray())list.Add(new PlexItem{Id=JStr(it,"Id"),Type=JStr(it,"Type"),Title=JStr(it,"Title"),Sub=JStr(it,"Sub"),Image=JStr(it,"Image"),Hub=JStr(it,"Hub")=="recent"?"recent":"continue"});
    itemsChanged=!mediaLoaded||list.Count!=plexItems.Count;
    for(int i=0;!itemsChanged&&i<list.Count;i++)itemsChanged=list[i].Id!=plexItems[i].Id||list[i].Image!=plexItems[i].Image||list[i].Hub!=plexItems[i].Hub;
    plexItems.Clear();plexItems.AddRange(list);mediaLoaded=true;
   }
  }}catch{}
  if(heroDemo&&plexItems.Count>0)ApplyHeroDemo();
  UpdateHero();
  bool idle=editing==""&&!coverPopupOpen&&!newsMenuOpen&&!factDetailOpen&&!forecastDetailOpen&&!lineDetailOpen;
  if(page==8&&(itemsChanged||nowChanged)&&idle)RebuildKeepFocus();
  else if(page==0&&prefWall==2&&(itemsChanged||nowChanged)&&idle)BuildPage();
  if(briefRoot!=null)UpdateBrief();
  WriteState("media_received:"+plexItems.Count+":"+(nowPlaying!=null));
 }
 // Redraw without moving the remote's focus to a different card when cards shift (for example when the Apple TV card disappears).
 void RebuildKeepFocus()
 {
  string keep=selected>=0&&selected<slots.Count?slots[selected].Action:"";int keepArea=area;
  BuildPage();
  if(keep!=""&&keepArea!=0&&keepArea!=3){int found=-1;for(int i=0;i<slots.Count;i++)if(slots[i].Action==keep){found=i;break;}
   if(found<0&&keep=="now_playing"){for(int i=0;i<slots.Count;i++)if(slots[i].Row>=0){found=i;break;}}
   if(found>=0){selected=found;area=keepArea;UpdateFocus(false);}}
 }
 double NowPosition()
 {
  var np=nowPlaying;if(np==null)return 0;double pos=np.Pos;
  DateTime t;if(np.State=="playing"&&DateTime.TryParse(np.Updated,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out t))pos+=(DateTime.UtcNow-t.ToUniversalTime()).TotalSeconds;
  return np.Dur>0?Math.Min(pos,np.Dur):pos;
 }
 bool heroDemo;
 // Test entry only (show:herodemo): pretend a Plex title is playing so the full-screen backdrop can be checked.
 void ApplyHeroDemo(){heroDemo=false;var it=plexItems.FirstOrDefault(x=>x.Image!="");if(it==null)return;nowPlaying=new NowItem{Source="Apple TV",App="Plex",Title=it.Title,Sub=it.Sub,State="playing",Image=it.Image,Dur=5400,Pos=2100,Updated=""};BuildPage();}
 void UpdateHero()
 {
  if(heroTitle==null)return;var np=nowPlaying;
  string art=np!=null&&np.Image!=""&&File.Exists(np.Image)?np.Image:"";
  bool onImage=art!="";
  if(heroBackdrop!=null){heroBackdrop.Opacity=onImage?1:0;if(onImage&&heroBackdropImage.ResourceUrl!=art)heroBackdropImage.ResourceUrl=art;}
  var big=onImage?white:ink;var soft=onImage?new Color(1,1,1,.82f):muted;
  heroTop.TextColor=soft;heroTitle.TextColor=big;heroSub.TextColor=soft;heroTime.TextColor=soft;
  if(np==null){
   heroTop.Text="ENTRÉE ACTIVE · HDMI 1";heroTitle.Text="Apple TV";heroTitle.PixelSize=104;heroSub.Text="Rien en lecture";
   heroBar.Opacity=0;heroTime.Text="";if(heroButton!=null)heroButton.Text="Passer sur l’Apple TV";SetHeroArt("");return;
  }
  heroTop.Text=(np.State=="playing"?"EN LECTURE":"EN PAUSE")+(np.App!=""?" · "+np.App.ToUpperInvariant():"");
  string title=Clip(np.Title,34);heroTitle.Text=title;heroTitle.PixelSize=title.Length>22?72:104;
  heroSub.Text=Clip(np.Sub,60);
  if(np.Dur>60){double pos=NowPosition();heroBar.Opacity=1;heroBar.BackgroundColor=onImage?new Color(1,1,1,.28f):Sf(.84f,.86f,.9f,1);heroFill.BackgroundColor=onImage?white:accent;heroFill.Size2D=new Size2D((int)(420*Math.Max(0,Math.Min(1,pos/np.Dur))),6);int left=(int)Math.Round((np.Dur-pos)/60);heroTime.Text=left<=0?"Bientôt fini":left>=60?"Reste "+left/60+" h "+(left%60).ToString("00"):"Reste "+left+" min";}
  else{heroBar.Opacity=0;heroTime.Text="";}
  if(heroButton!=null)heroButton.Text=np.Source=="Apple TV"?"Revenir à l’Apple TV":"Ouvrir l’Apple TV";
  SetHeroArt(art);
 }
 // Full-screen backdrop for the Accueil: the artwork of what is playing, enlarged soft, darkened on the left for the title
 // and fading into the page under the hero so the rows stay on the normal background.
 void BuildHeroBackdrop()
 {
  heroBackdrop=Add(new View{Size2D=new Size2D(1920,1080),Opacity=0});
  heroBackdropImage=new ImageView{Size2D=new Size2D(1920,1080),FittingMode=FittingModeType.ScaleToFill,DesiredWidth=160,DesiredHeight=90};heroBackdrop.Add(heroBackdropImage);
  string scrim=HeroScrimFile();if(scrim!="")heroBackdrop.Add(new ImageView{ResourceUrl=scrim,Size2D=new Size2D(1920,1080)});
 }
 string HeroScrimFile()
 {
  try{
   string bg=prefWall==4?SeventiesBg():dark?"#0B0D14":"#EEF0F4";
   string file=System.IO.Path.Combine(Current.DirectoryInfo.Data,"hero-scrim3-"+(dark?"dark":"light")+(prefWall==4?"-70":"")+".svg");
   if(!File.Exists(file))File.WriteAllText(file,"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1920\" height=\"1080\" viewBox=\"0 0 1920 1080\"><defs>"
    +"<linearGradient id=\"l\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\"><stop offset=\"0\" stop-color=\"#000\" stop-opacity=\".78\"/><stop offset=\".55\" stop-color=\"#000\" stop-opacity=\".25\"/><stop offset=\"1\" stop-color=\"#000\" stop-opacity=\".05\"/></linearGradient>"
    +"<linearGradient id=\"b\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\".52\" stop-color=\""+bg+"\" stop-opacity=\"0\"/><stop offset=\".70\" stop-color=\""+bg+"\" stop-opacity=\".94\"/><stop offset=\".75\" stop-color=\""+bg+"\" stop-opacity=\"1\"/></linearGradient></defs>"
    +"<rect width=\"1920\" height=\"1080\" fill=\"url(#l)\"/><rect width=\"1920\" height=\"1080\" fill=\"url(#b)\"/></svg>");
   return file;
  }catch{return "";}
 }
 static string Clip(string s,int max)
 {
  if(s.Length<=max)return s;int cut=s.LastIndexOf(' ',max-1);
  return (cut>max/2?s.Substring(0,cut):s.Substring(0,max-1)).TrimEnd(',',' ','·','-',':')+"…";
 }
 void SetHeroArt(string path)
 {
  if(heroArt==null||path==heroArtPath)return;heroArtPath=path;
  while(heroArt.ChildCount>0){var child=heroArt.GetChildAt(0);heroArt.Remove(child);child.Dispose();}
  // no artwork (YouTube, nothing playing): no card at all, the wallpaper carries the hero
  heroArt.Opacity=path!=""?1:0;
  if(path!="")heroArt.Add(new ImageView{ResourceUrl=path,Size2D=new Size2D(464,293),FittingMode=FittingModeType.ScaleToFill,SamplingMode=SamplingModeType.BoxThenLanczos,DesiredWidth=928,DesiredHeight=586});
 }
 View MediaCard(int x,int y,int w,int h,string image,bool fit,string badge,string placeholder="PLEX")
 {
  var card=Panel(x+w/2,y+h/2,w,h,new Color(.09f,.10f,.14f,1),18);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
  if(image!=""&&File.Exists(image)){
   if(fit){
    card.Add(new ImageView{ResourceUrl=image,Size2D=new Size2D(w,h),FittingMode=FittingModeType.ScaleToFill,DesiredWidth=40,DesiredHeight=22,CornerRadius=new Vector4(18,18,18,18)});
    card.Add(new View{Size2D=new Size2D(w,h),BackgroundColor=new Color(0,0,0,.30f),CornerRadius=new Vector4(18,18,18,18)});
    int ph=h-28,pw=ph*2/3;
    card.Add(new ImageView{ResourceUrl=image,Position2D=new Position2D((w-pw)/2,14),Size2D=new Size2D(pw,ph),FittingMode=FittingModeType.ScaleToFill,SamplingMode=SamplingModeType.BoxThenLanczos,DesiredWidth=pw*2,DesiredHeight=ph*2,CornerRadius=new Vector4(10,10,10,10)});
   }else card.Add(new ImageView{ResourceUrl=image,Size2D=new Size2D(w,h),FittingMode=FittingModeType.ScaleToFill,SamplingMode=SamplingModeType.BoxThenLanczos,DesiredWidth=w*3/2,DesiredHeight=h*3/2,CornerRadius=new Vector4(18,18,18,18)});
  }else{var p=Label(placeholder,20,0,w-40,h,34,new Color(1,1,1,.42f),"Semibold");p.HorizontalAlignment=HorizontalAlignment.Center;card.Add(p);}
  if(badge!=""){
   var chip=new View{Position2D=new Position2D(14,14),Size2D=new Size2D(badge.Length*11+34,34),BackgroundColor=accent,CornerRadius=new Vector4(17,17,17,17)};card.Add(chip);
   var t=Label(badge,0,0,badge.Length*11+34,34,15,white,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;chip.Add(t);
  }
  return card;
 }
 void BuildPlex()
 {
  if(!mediaLoaded&&DateTime.Now-lastHubRequest>=TimeSpan.FromSeconds(15))RequestExtra(true,true,false);
  int cw=328,ch=185,gx=30;
  var cont=plexItems.Where(i=>i.Hub!="recent").ToList();var recent=plexItems.Where(i=>i.Hub=="recent").ToList();
  // row 0: what is playing, then Continue watching / On deck
  Text(nowPlaying!=null?"En cours et à reprendre":"Reprendre",80,232,1100,44,30,ink,"Semibold");
  var src=Text("PlexAir",900,236,940,36,19,muted,"Medium");src.HorizontalAlignment=HorizontalAlignment.End;
  int n=0;
  if(nowPlaying!=null){
   var np=nowPlaying;var card=MediaCard(80,284,cw,ch,np.Image,true,np.State=="playing"?"EN LECTURE":"EN PAUSE",np.App!=""?np.App:np.Source);
   Text(Clip(np.Title,26),80,284+ch+10,cw,32,22,ink,"Semibold");Text(Clip((np.App!=""?np.App+" · ":"")+np.Sub,34),80,284+ch+42,cw,28,17,muted,"Medium");
   slots.Add(new Slot{Card=card,Name=np.Title,Action="now_playing",Description=np.Source=="Apple TV"?"OK pour revenir sur l’Apple TV":"OK pour ouvrir Plex",Row=0,X=80,Y=284,W=cw,H=ch});n=1;
  }
  for(int i=0;i<cont.Count&&n<5;i++,n++)PlexCard(cont[i],80+n*(cw+gx),284,cw,ch,0,"OK pour reprendre dans Plex");
  if(n==0)Text(mediaLoaded?"Rien à reprendre sur Plex pour le moment.":plexOffline?"Ouvrez Plex une fois sur l’Apple TV ou la TV : la liste sera ensuite gardée ici.":"Chargement de Plex…",80,290,1600,50,26,muted,"Medium");
  // row 1: recently added movies and episodes
  Text("Ajoutés récemment",80,580,1100,44,30,ink,"Semibold");
  for(int i=0;i<recent.Count&&i<5;i++)PlexCard(recent[i],80+i*(cw+gx),632,cw,ch,1,"OK pour lancer dans Plex");
  if(recent.Count==0)Text(mediaLoaded||plexOffline?"La liste apparaîtra la prochaine fois que Plex sera ouvert.":"Chargement de Plex…",80,638,1500,50,24,muted,"Medium");
 }
 void PlexCard(PlexItem it,int x,int y,int cw,int ch,int row,string hint)
 {
  var card=MediaCard(x,y,cw,ch,it.Image,it.Type=="movie","");
  Text(Clip(it.Title,26),x,y+ch+10,cw,32,22,ink,"Semibold");Text(Clip(it.Sub,34),x,y+ch+42,cw,28,17,muted,"Medium");
  slots.Add(new Slot{Card=card,Name=it.Title,Action="plex_play:"+it.Id,Description=(it.Sub!=""?it.Sub+" · ":"")+hint,Row=row,X=x,Y=y,W=cw,H=ch});
 }
 // ---- Vélos: Vélib' stations around Bel-Air and Lime bikes on a small plan centred on the metro ----
 // Plan navigation: → from a station tile enters the plan, arrows hop between Lime bikes and stations, Back returns.
 int mapSel=-1;
 List<int[]> MapTargets(){var t=new List<int[]>();for(int i=0;i<velib.Count&&i<4;i++)t.Add(new[]{velib[i].Dx,velib[i].Dy,1,i});for(int i=0;i<limePts.Count;i++)t.Add(new[]{limePts[i][0],limePts[i][1],0,i});return t;}
 void EnterMap(){var t=MapTargets();if(t.Count==0)return;int best=0;double bd=double.MaxValue;int st=selected>=0&&selected<slots.Count?slots[selected].Row:0;
  if(st>=0&&st<velib.Count){for(int i=0;i<t.Count;i++)if(t[i][2]==1&&t[i][3]==st){best=i;break;}}else for(int i=0;i<t.Count;i++){double d=Math.Abs(t[i][0])+Math.Abs(t[i][1]);if(d<bd){bd=d;best=i;}}
  mapSel=best;BuildPage();}
 void HandleMapKey(string k)
 {
  var t=MapTargets();if(t.Count==0||mapSel>=t.Count){mapSel=-1;BuildPage();return;}
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){int row=t[mapSel][2]==1?t[mapSel][3]:0;mapSel=-1;BuildPage();var r=Row(row);if(r.Count>0){selected=r[0];UpdateFocus();}return;}
  int dx=k=="Left"?-1:k=="Right"?1:0,dy=k=="Up"?1:k=="Down"?-1:0;if(dx==0&&dy==0)return;
  var cur=t[mapSel];int best=-1;double bs=double.MaxValue;
  for(int i=0;i<t.Count;i++){if(i==mapSel)continue;double ax=t[i][0]-cur[0],ay=t[i][1]-cur[1];double along=ax*dx+ay*dy,across=Math.Abs(ax*dy-ay*dx);if(along<=4)continue;double score=along+across*2.2;if(score<bs){bs=score;best=i;}}
  if(best<0){if(k=="Left"){int row=cur[2]==1?cur[3]:0;mapSel=-1;BuildPage();var r=Row(row);if(r.Count>0){selected=r[0];UpdateFocus();}}return;}
  mapSel=best;BuildPage();
 }
 void ApplyMapMode()
 {
  foreach(var sl in slots){sl.Card.Scale=new Vector3(1,1,1);sl.Card.BorderlineWidth=0;}
  detail.Text="Flèches : parcourir la carte · Retour : revenir aux stations";
 }
 const int bikeMapX=560,bikeMapY=290,bikeMapW=1280,bikeMapH=690,bikeMapR=430;
 // Street map tiles per plan (key "theme|width|height|radius for 400 m"), from the helper's cache.
 readonly Dictionary<string,Tuple<List<Tuple<string,int,int>>,int>> basemaps=new Dictionary<string,Tuple<List<Tuple<string,int,int>>,int>>();readonly Dictionary<string,DateTime> basemapAsked=new Dictionary<string,DateTime>();
 static bool Demo(){try{return File.Exists("/tmp/quiet-demo");}catch{return false;}}
 string MapKey(int w,int h,double r){return (Demo()?"demo-":"")+(dark?"dark":"light")+"|"+w+"|"+h+"|"+r.ToString("0.###",CultureInfo.InvariantCulture);}
 readonly HashSet<string> basemapPartial=new HashSet<string>();readonly Dictionary<string,double[]> basemapCenter=new Dictionary<string,double[]>();
 // Adds the street map to a plan panel (asks for it the first time), with a dark veil in the dark theme.
 void AddBasemap(View map,int mw,int mh,double r,string target="",double wantWx=-1,double wantWy=-1,double pxPerW=0)
 {
  string key=MapKey(mw,mh,r)+(target!=""?"|"+target:"");
  bool have=basemaps.TryGetValue(key,out var bm);
  // ask again after 30 s, or after 6 s when the last answer still had missing tiles
  if((!have||basemapPartial.Contains(key))&&(!basemapAsked.TryGetValue(key,out var t)||DateTime.Now-t>=TimeSpan.FromSeconds(have?6:30))){basemapAsked[key]=DateTime.Now;RequestBasemap(key);}
  if(!have){
   // While the right view loads, show the closest loaded view of the same plan, scaled and shifted (blurry for a moment, never empty).
   string prefix=(Demo()?"demo-":"")+(dark?"dark":"light")+"|"+mw+"|"+mh+"|",suffix=target!=""?"|"+target:"";Tuple<List<Tuple<string,int,int>>,int> best=null;double bk=0;double[] bc=null;
   foreach(var kv in basemaps){if(!kv.Key.StartsWith(prefix))continue;var rest=kv.Key.Substring(prefix.Length);int bar=rest.IndexOf('|');string rs=bar<0?rest:rest.Substring(0,bar),sx=bar<0?"":rest.Substring(bar);
    basemapCenter.TryGetValue(kv.Key,out var oc);bool sameCentre=sx==suffix;if(!sameCentre&&(oc==null||wantWx<0||pxPerW<=0))continue;
    if(!double.TryParse(rs,NumberStyles.Float,CultureInfo.InvariantCulture,out var or)||or<=0)continue;double k=r/or;if(k<0.24||k>4.2)continue;
    double shift=sameCentre?0:Math.Abs(oc[0]-wantWx)*pxPerW+Math.Abs(oc[1]-wantWy)*pxPerW;if(shift>mw)continue;
    double score=Math.Abs(Math.Log(k))+shift/mw;if(best==null||score<Math.Abs(Math.Log(bk))+(bc==null?0:(Math.Abs(bc[0]-wantWx)+Math.Abs(bc[1]-wantWy))*pxPerW/mw)){best=kv.Value;bk=k;bc=sameCentre?null:oc;}}
   if(best!=null){int ox=bc==null?0:(int)Math.Round((bc[0]-wantWx)*pxPerW),oy=bc==null?0:(int)Math.Round((bc[1]-wantWy)*pxPerW);
    foreach(var tile in best.Item1){int sz=(int)Math.Ceiling(best.Item2*bk)+1;map.Add(new ImageView{ResourceUrl=tile.Item1,Position2D=new Position2D(mw/2+ox+(int)Math.Floor(tile.Item2*bk),mh/2+oy+(int)Math.Floor(tile.Item3*bk)),Size2D=new Size2D(sz,sz)});}
    if(dark)map.Add(new View{Size2D=new Size2D(mw,mh),BackgroundColor=new Color(.05f,.07f,.11f,.58f)});}
   return;
  }
  foreach(var tile in bm.Item1)map.Add(new ImageView{ResourceUrl=tile.Item1,Position2D=new Position2D(mw/2+tile.Item2,mh/2+tile.Item3),Size2D=new Size2D(bm.Item2,bm.Item2),DesiredWidth=bm.Item2,DesiredHeight=bm.Item2});
  if(dark&&bm.Item1.Count>0)map.Add(new View{Size2D=new Size2D(mw,mh),BackgroundColor=new Color(.05f,.07f,.11f,.58f)});
 }
 void BuildBikes()
 {
  if(!mobLoaded&&DateTime.Now-lastMobilityRequest>=TimeSpan.FromSeconds(10))RequestExtra(false,false,false,false,true);
  Text("Vélib’ autour de chez vous",80,232,1000,44,30,ink,"Semibold");
  var src=Text(mobAt!=""?"Vélib’ Métropole et Lime · à "+mobAt:"",1140,236,700,36,19,muted,"Medium");src.HorizontalAlignment=HorizontalAlignment.End;
  var mechC=new Color(.24f,.66f,.36f,1);var eC=new Color(.18f,.50f,.93f,1);var pinInk=new Color(.10f,.12f,.16f,1);
  // four compact station tiles, numbered like the pins on the plan
  int cw=460,ch=160,gy=16;
  if(velib.Count==0)Text(mobLoaded?"Stations Vélib’ indisponibles.":"Chargement des stations…",80,300,460,50,24,muted,"Medium");
  for(int i=0;i<velib.Count&&i<4;i++){
   var st=velib[i];int x=80,y=290+i*(ch+gy);
   var card=Panel(x+cw/2,y+ch/2,cw,ch,GlassFill(),22);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
   var badge=new View{Position2D=new Position2D(20,18),Size2D=new Size2D(38,38),CornerRadius=new Vector4(19,19,19,19),BackgroundColor=white,BorderlineWidth=4,BorderlineColor=eC};card.Add(badge);
   var bn=Label((i+1).ToString(),0,0,38,38,19,pinInk,"Semibold");bn.HorizontalAlignment=HorizontalAlignment.Center;badge.Add(bn);
   card.Add(Label(Clip(st.Name,26),70,14,cw-140,34,23,ink,"Semibold"));
   var dist=Label(st.Open?st.Dist+" m":"fermée",cw-110,16,90,30,18,st.Open?muted:new Color(.89f,.22f,.17f,1),"Medium");dist.HorizontalAlignment=HorizontalAlignment.End;card.Add(dist);
   int sw=(cw-40)/3;int[] vals={st.Mech,st.Ebike,st.Docks};string[] caps={"mécaniques","électriques","places"};Color[] cols={mechC,eC,ink};
   for(int k=0;k<3;k++){card.Add(Label(vals[k].ToString(),20+k*sw,58,sw,56,46,vals[k]==0?muted:cols[k],"Display"));card.Add(Label(caps[k],20+k*sw,114,sw,26,17,muted,"Medium"));}
   slots.Add(new Slot{Card=card,Name=st.Name,Action="",Description=st.Dist+" m · "+st.Mech+" mécaniques · "+st.Ebike+" électriques · "+st.Docks+" places",Row=i,X=x,Y=y,W=cw,H=ch});
  }
  // plan on a street map: Bel-Air metro in the middle, rings at 200 and 400 m, Lime bikes, numbered Vélib' stations
  int mw=bikeMapW,mh=bikeMapH,cx=mw/2,cy=mh/2,R=bikeMapR;
  var map=Panel(bikeMapX,bikeMapY,mw,mh,dark?new Color(.10f,.13f,.18f,1):new Color(.90f,.92f,.94f,1),26);map.ClippingMode=ClippingModeType.ClipChildren;
  AddBasemap(map,mw,mh,R);
  var ringC=dark?new Color(1,1,1,.55f):new Color(.10f,.14f,.22f,.55f);
  foreach(int rr in new[]{R,R/2}){map.Add(new View{Position2D=new Position2D(cx-rr,cy-rr),Size2D=new Size2D(rr*2,rr*2),CornerRadius=new Vector4(rr,rr,rr,rr),BackgroundColor=new Color(.18f,.50f,.93f,rr==R?.05f:.06f),BorderlineWidth=2,BorderlineColor=ringC});
   var rl=Label(rr==R?"400 m":"200 m",cx-40,cy-rr-26,80,24,15,ringC,"Semibold");rl.HorizontalAlignment=HorizontalAlignment.Center;map.Add(rl);}
  var limeC=new Color(.20f,.80f,.30f,1);
  foreach(var q in limePts){int px=cx+q[0]*R/400,py=cy-q[1]*R/400;map.Add(new View{Position2D=new Position2D(px-8,py-8),Size2D=new Size2D(16,16),CornerRadius=new Vector4(8,8,8,8),BackgroundColor=limeC,BorderlineWidth=2,BorderlineColor=white});}
  for(int i=0;i<velib.Count&&i<4;i++){var st=velib[i];int px=cx+st.Dx*R/400,py=cy-st.Dy*R/400;
   var pin=new View{Position2D=new Position2D(px-15,py-15),Size2D=new Size2D(30,30),CornerRadius=new Vector4(15,15,15,15),BackgroundColor=white,BorderlineWidth=3.5f,BorderlineColor=eC};map.Add(pin);
   var pl=Label((i+1).ToString(),0,0,30,30,16,pinInk,"Semibold");pl.HorizontalAlignment=HorizontalAlignment.Center;pin.Add(pl);}
  int mx6=cx+metroDx*R/400,my6=cy-metroDy*R/400;
  map.Add(new View{Position2D=new Position2D(mx6-13,my6-13),Size2D=new Size2D(26,26),CornerRadius=new Vector4(13,13,13,13),BackgroundColor=HexColor("#83C491"),BorderlineWidth=2.5f,BorderlineColor=white});
  var six=Label("6",mx6-13,my6-13,26,26,15,TileTitleColor("#83C491"),"Semibold");six.HorizontalAlignment=HorizontalAlignment.Center;map.Add(six);
  var me=new View{Position2D=new Position2D(cx-15,cy-15),Size2D=new Size2D(30,30),CornerRadius=new Vector4(15,15,15,15),BackgroundColor=new Color(.94f,.33f,.31f,1),BorderlineWidth=3.5f,BorderlineColor=white};map.Add(me);
  me.Add(new View{Position2D=new Position2D(10,10),Size2D=new Size2D(10,10),CornerRadius=new Vector4(5,5,5,5),BackgroundColor=white});
  var targets=MapTargets();if(mapSel>=targets.Count)mapSel=targets.Count>0?0:-1;
  if(mapSel>=0){
   var tg=targets[mapSel];int px=cx+tg[0]*R/400,py=cy-tg[1]*R/400;
   map.Add(new View{Position2D=new Position2D(px-24,py-24),Size2D=new Size2D(48,48),CornerRadius=new Vector4(24,24,24,24),BackgroundColor=new Color(0,0,0,0),BorderlineWidth=4,BorderlineColor=accent});
   int dist=(int)Math.Round(Math.Sqrt(tg[0]*tg[0]+tg[1]*tg[1]));string title,line1,line2;
   if(tg[2]==1){var st=velib[tg[3]];title=(tg[3]+1)+" · "+Clip(st.Name,24);line1=st.Mech+" mécaniques · "+st.Ebike+" électriques";line2=st.Docks+" places libres · à "+st.Dist+" m";}
   else{var q=limePts[tg[3]];title=q[3]==1?"Trottinette Lime":q[3]==0?"Vélo électrique Lime":"Lime";line1=q[2]>=0?"Autonomie ≈ "+(q[2]/1000.0).ToString(q[2]<10000?"0.0":"0",frCulture)+" km":"Autonomie inconnue";line2="à "+dist+" m de chez vous";}
   int bw=400,bh=118,bx=Math.Max(16,Math.Min(mw-bw-16,px-bw/2)),by=py-bh-34<150?py+34:py-bh-34;
   var bub=new View{Position2D=new Position2D(bx,by),Size2D=new Size2D(bw,bh),CornerRadius=new Vector4(18,18,18,18),BackgroundColor=Sf(1,1,1,.96f),BorderlineWidth=3,BorderlineColor=accent};map.Add(bub);
   bub.Add(Label(title,20,12,bw-40,34,23,ink,"Semibold"));bub.Add(Label(line1,20,48,bw-40,30,20,tg[2]==0?new Color(.15f,.62f,.25f,1):ink,"Semibold"));bub.Add(Label(line2,20,80,bw-40,28,17,muted,"Medium"));
  }
  // Lime summary and legend on frosted boxes
  var box=new View{Position2D=new Position2D(20,20),Size2D=new Size2D(470,112),CornerRadius=new Vector4(18,18,18,18),BackgroundColor=Sf(1,1,1,.92f)};map.Add(box);
  box.Add(Label("LIME · VÉLOS ÉLECTRIQUES",20,12,430,24,15,muted,"Semibold"));
  box.Add(Label(limeCount<0?(mobLoaded?"Indisponible":"…"):limeCount+" à moins de 400 m",20,36,430,44,32,ink,"Display"));
  box.Add(Label(limeNear>=0?"Le plus proche à "+limeNear+" m":"",20,78,430,26,17,muted,"Medium"));
  var leg=new View{Position2D=new Position2D(20,mh-62),Size2D=new Size2D(760,42),CornerRadius=new Vector4(14,14,14,14),BackgroundColor=Sf(1,1,1,.92f)};map.Add(leg);
  leg.Add(new View{Position2D=new Position2D(16,14),Size2D=new Size2D(14,14),CornerRadius=new Vector4(7,7,7,7),BackgroundColor=limeC});
  leg.Add(Label("Lime",38,6,70,30,16,ink,"Medium"));
  leg.Add(new View{Position2D=new Position2D(112,9),Size2D=new Size2D(24,24),CornerRadius=new Vector4(12,12,12,12),BackgroundColor=white,BorderlineWidth=3,BorderlineColor=eC});
  leg.Add(Label("Station Vélib’ (n° de la tuile)",144,6,300,30,16,ink,"Medium"));
  leg.Add(new View{Position2D=new Position2D(444,9),Size2D=new Size2D(24,24),CornerRadius=new Vector4(12,12,12,12),BackgroundColor=new Color(.94f,.33f,.31f,1),BorderlineWidth=3,BorderlineColor=white});
  leg.Add(Label("Vous",476,6,80,30,16,ink,"Medium"));
  leg.Add(new View{Position2D=new Position2D(556,9),Size2D=new Size2D(24,24),CornerRadius=new Vector4(12,12,12,12),BackgroundColor=HexColor("#83C491")});
  leg.Add(Label("Métro Bel-Air",588,6,160,30,16,ink,"Medium"));
  if(mapSel<0){var tip=new View{Position2D=new Position2D(mw-370,20),Size2D=new Size2D(350,42),CornerRadius=new Vector4(14,14,14,14),BackgroundColor=Sf(1,1,1,.92f)};map.Add(tip);var tl=Label("→ pour explorer la carte",0,6,350,30,17,ink,"Medium");tl.HorizontalAlignment=HorizontalAlignment.Center;tip.Add(tl);}
  var attr=Label("Plan IGN © IGN · © OpenStreetMap",mw-330,mh-36,310,26,14,dark?new Color(1,1,1,.6f):new Color(0,0,0,.55f),"Medium");attr.HorizontalAlignment=HorizontalAlignment.End;map.Add(attr);
 }
 void RequestBasemap(string key)
 {
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_basemap_request",key);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
 }
 void ReceiveBasemap(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){
   var r=doc.RootElement;var list=new List<Tuple<string,int,int>>();
   foreach(var t in r.GetProperty("Tiles").EnumerateArray())list.Add(Tuple.Create(t[0].GetString()??"",t[1].GetInt32(),t[2].GetInt32()));
   string key=JStr(r,"Key");if(list.Count>0&&!key.EndsWith("|metro")){basemaps[key]=Tuple.Create(list,(int)JNum(r,"Size"));if(JNum(r,"Missing")>0)basemapPartial.Add(key);else basemapPartial.Remove(key);basemapAsked[key]=DateTime.Now;if(JNum(r,"CWx")>0)basemapCenter[key]=new[]{JNum(r,"CWx"),JNum(r,"CWy")};}
  }}catch{}
  if((page==10||page==11&&findUnlocked)&&editing=="")RebuildKeepFocus();
 }
 // ---- Localiser: devices from Find My (Home Assistant's iCloud integration), behind a 4-digit PIN ----
 sealed class FindItem { public string Id="",Name="",Bs="",Model="",State="",Zone="",Sound="",Located=""; public int Bat=-1,Acc,Dx,Dy; public long Dist; public double Wx,Wy; }
 double homeWx=-1,homeWy=-1;
 readonly List<FindItem> findItems=new List<FindItem>();bool findUnlocked,findLoaded;string pinEntry="",pinFirst=null,pinMsg="",findFocus="",pendingFind;int pinFails;DateTime pinLockUntil=DateTime.MinValue,lastFindRequest=DateTime.MinValue;string ringAsk="";int ringSel;
 const int findMapX=560,findMapY=290,findMapW=1280,findMapH=690,findMapR=170,findFocusR=300;
 // Zoom steps: pixels for 400 m. -1 = automatic (overview or device).
 static readonly double[] findZooms={0.03,0.07,0.15,0.35,0.75,1.5,3,6,12,25,45,80,120,170,300,520,900};int findZoom=-1;bool findPan;double panWx,panWy;
 void FindZoom(int dir){int cur=findZoom>=0?findZoom:Array.IndexOf(findZooms,(double)(findFocus!=""?findFocusR:findMapR));findZoom=Math.Max(0,Math.Min(findZooms.Length-1,cur+dir));RebuildKeepFocus();}
 string PinPath(){try{return System.IO.Path.Combine(Current.DirectoryInfo.Data,"find-pin.txt");}catch{return "";}}
 static string PinHash(string salt,string pin){using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(salt+":"+pin)));}
 bool PinExists(){try{return File.Exists(PinPath());}catch{return false;}}
 bool PinCheck(string pin){try{var p=File.ReadAllText(PinPath()).Trim().Split(':');return p.Length==2&&PinHash(p[0],pin)==p[1];}catch{return false;}}
 void PinSave(string pin){try{string salt=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));File.WriteAllText(PinPath(),salt+":"+PinHash(salt,pin));}catch{}}
 void PinKey(string k)
 {
  if(DateTime.Now<pinLockUntil){pinMsg="Trop d’essais · patientez "+(int)(pinLockUntil-DateTime.Now).TotalSeconds+" s";PinFocusOne();return;}
  if(k=="back"){Home("find_back");return;}
  if(k=="del"){if(pinEntry.Length>0)pinEntry=pinEntry.Substring(0,pinEntry.Length-1);PinFocusOne();return;}
  if(pinEntry.Length<4)pinEntry+=k;
  if(pinEntry.Length<4){PinFocusOne();return;}
  string entry=pinEntry;pinEntry="";
  if(!PinExists()){
   if(pinFirst==null){pinFirst=entry;pinMsg="";}
   else if(pinFirst==entry){PinSave(entry);pinFirst=null;Unlock();return;}
   else{pinFirst=null;pinMsg="Les deux codes sont différents, recommencez.";}
  }
  else if(PinCheck(entry)){Unlock();return;}
  else{pinFails++;pinMsg=pinFails>=3?"Code incorrect · patientez 30 s":"Code incorrect";if(pinFails>=3){pinFails=0;pinLockUntil=DateTime.Now.AddSeconds(30);}}
  PinFocusOne();
 }
 // The PIN pad never shows which digit was chosen: focus goes back to 1 after every key, and the footer names no digit.
 void PinFocusOne(){BuildPage();for(int i=0;i<slots.Count;i++)if(slots[i].Action=="pin:1"){selected=i;area=1;break;}UpdateFocus(false);}
 void Unlock(){findUnlocked=true;pinFails=0;pinMsg="";area=1;selected=0;if(!findLoaded||DateTime.Now-lastFindRequest>=TimeSpan.FromSeconds(20))RequestExtra(false,false,false,false,false,true);BuildPage();}
 void BuildFind()
 {
  if(!findUnlocked&&Demo()){findUnlocked=true;findItems.Clear();findLoaded=false;RequestExtra(false,false,false,false,false,true);}
  if(!findUnlocked){BuildPinPad();return;}
  Text("Appareils et objets",80,232,1000,44,30,ink,"Semibold");
  var src=Text("Localiser (iCloud)"+(findLoaded?"":" · chargement…"),1140,236,700,36,19,muted,"Medium");src.HorizontalAlignment=HorizontalAlignment.End;
  // iPhones first, then whatever is at home, then by distance
  var items=findItems.OrderBy(f=>f.Model.StartsWith("iPhone")||f.Name.Contains("iPhone")?0:1).ThenBy(f=>f.State=="home"?0:1).ThenBy(f=>f.Dist).Take(5).ToList();
  var focus=items.FirstOrDefault(f=>f.Id==findFocus);if(focus==null)findFocus="";
  int cw=460,ch=124,gy=12,mw=findMapW,mh=findMapH,cx=mw/2,cy=mh/2;double R=findZoom>=0?findZooms[findZoom]:(focus!=null?findFocusR:findMapR);var pinInk=new Color(.10f,.12f,.16f,1);
  // plan centre: home, or the chosen device; every position in Web Mercator so far devices land right at any zoom
  double cWx=(focus!=null&&focus.Wx>0?focus.Wx:homeWx)+panWx,cWy=Math.Max(0.5,Math.Min(255.5,(focus!=null&&focus.Wx>0?focus.Wy:homeWy)+panWy));
  double cLat=Math.Atan(Math.Sinh(Math.PI*(1-2*cWy/256)));double pxPerW=R/400.0*156543.03392*Math.Cos(cLat);
  bool panned=panWx!=0||panWy!=0;string mapTarget=panned?"pt:"+cWx.ToString("0.#########",CultureInfo.InvariantCulture)+","+cWy.ToString("0.#########",CultureInfo.InvariantCulture):focus!=null?focus.Id:"";
  Func<double,double,int[]> toPx=(wx,wy)=>{double px=cx+(wx-cWx)*pxPerW,py=cy+(wy-cWy)*pxPerW;return Math.Abs(px)>1e6||Math.Abs(py)>1e6?new[]{-99999,-99999}:new[]{(int)Math.Round(px),(int)Math.Round(py)};};
  if(items.Count==0)Text(findLoaded?"Aucun appareil localisé.":"Chargement…",80,300,460,50,24,muted,"Medium");
  var map=Panel(findMapX,findMapY,mw,mh,dark?new Color(.10f,.13f,.18f,1):new Color(.90f,.92f,.94f,1),26);map.ClippingMode=ClippingModeType.ClipChildren;
  AddBasemap(map,mw,mh,R,mapTarget,cWx,cWy,pxPerW);
  var ringC=dark?new Color(1,1,1,.45f):new Color(.10f,.14f,.22f,.45f);
  if(focus==null&&!panned)foreach(int m in new[]{500,1000}){int rr=(int)(m*R/400);if(rr<14||rr>mw)continue;map.Add(new View{Position2D=new Position2D(cx-rr,cy-rr),Size2D=new Size2D(rr*2,rr*2),CornerRadius=new Vector4(rr,rr,rr,rr),BackgroundColor=new Color(0,0,0,0),BorderlineWidth=2,BorderlineColor=ringC});
   var rl=Label(m==1000?"1 km":"500 m",cx-40,cy-rr-24,80,22,14,ringC,"Semibold");rl.HorizontalAlignment=HorizontalAlignment.Center;map.Add(rl);}
  if(homeWx>0){var hp=toPx(homeWx,homeWy);if(hp[0]>0&&hp[0]<mw&&hp[1]>0&&hp[1]<mh){
   var me=new View{Position2D=new Position2D(hp[0]-15,hp[1]-15),Size2D=new Size2D(30,30),CornerRadius=new Vector4(15,15,15,15),BackgroundColor=new Color(.94f,.33f,.31f,1),BorderlineWidth=3.5f,BorderlineColor=white};map.Add(me);
   me.Add(new View{Position2D=new Position2D(10,10),Size2D=new Size2D(10,10),CornerRadius=new Vector4(5,5,5,5),BackgroundColor=white});}}
  if(focus!=null&&!panned){int ar=(int)Math.Max(18,Math.Min(260,focus.Acc*R/400));map.Add(new View{Position2D=new Position2D(cx-ar,cy-ar),Size2D=new Size2D(ar*2,ar*2),CornerRadius=new Vector4(ar,ar,ar,ar),BackgroundColor=new Color(accent.R,accent.G,accent.B,.16f),BorderlineWidth=2,BorderlineColor=new Color(accent.R,accent.G,accent.B,.6f)});}
  for(int i=0;i<items.Count;i++){
   var it=items[i];int x=80,y=290+i*(ch+gy);
   var card=Panel(x+cw/2,y+ch/2,cw,ch,GlassFill(),22);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
   var badge=new View{Position2D=new Position2D(18,16),Size2D=new Size2D(34,34),CornerRadius=new Vector4(17,17,17,17),BackgroundColor=white,BorderlineWidth=3.5f,BorderlineColor=accent};card.Add(badge);
   var bn=Label((i+1).ToString(),0,0,34,34,17,pinInk,"Semibold");bn.HorizontalAlignment=HorizontalAlignment.Center;badge.Add(bn);
   card.Add(Label(Clip(it.Name,24),64,12,cw-170,34,22,ink,"Semibold"));
   if(it.Bat>=0){bool chg=it.Bs.ToLowerInvariant().Contains("charg")&&!it.Bs.ToLowerInvariant().StartsWith("not");var bl=Label((chg?"⚡ ":"")+it.Bat+" %",cw-110,14,92,30,19,it.Bat<20&&!chg?new Color(.89f,.22f,.17f,1):muted,"Semibold");bl.HorizontalAlignment=HorizontalAlignment.End;card.Add(bl);}
   card.Add(Label(FindPlace(it),64,48,cw-82,30,19,it.State=="home"?new Color(.24f,.66f,.36f,1):ink,"Medium"));
   card.Add(Label(Clip((it.Model!=""?it.Model+" · ":"")+FindAge(it.Located),44),64,80,cw-82,28,16,muted,"Medium"));
   bool same=focus!=null&&it.Id==focus.Id;var ip=it.Wx>0?toPx(it.Wx,it.Wy):new[]{-99999,-99999};
   bool onMap=ip[0]>10&&ip[0]<mw-10&&ip[1]>10&&ip[1]<mh-10;
   if(same){card.BackgroundColor=new Color(accent.R,accent.G,accent.B,dark?.30f:.18f);}
   if(onMap){int px=ip[0],py=ip[1];var pin=new View{Position2D=new Position2D(px-15,py-15),Size2D=new Size2D(30,30),CornerRadius=new Vector4(15,15,15,15),BackgroundColor=white,BorderlineWidth=3.5f,BorderlineColor=accent};map.Add(pin);
    var pl=Label((i+1).ToString(),0,0,30,30,16,pinInk,"Semibold");pl.HorizontalAlignment=HorizontalAlignment.Center;pin.Add(pl);}
   slots.Add(new Slot{Card=card,Name=it.Name,Action="find:"+it.Id,Description=(same?"OK : revenir à la maison":"OK : centrer la carte")+(it.Sound!=""?" · appui long : faire sonner":""),Row=i,X=x,Y=y,W=cw,H=ch});
  }
  if(focus!=null){
   var box=new View{Position2D=new Position2D(20,20),Size2D=new Size2D(560,112),CornerRadius=new Vector4(18,18,18,18),BackgroundColor=Sf(1,1,1,.94f)};map.Add(box);
   box.Add(Label(Clip(focus.Name,30),20,12,520,36,26,ink,"Semibold"));
   box.Add(Label(FindPlace(focus)+(focus.Acc>0?" · précision "+focus.Acc+" m":""),20,48,520,28,18,ink,"Medium"));
   box.Add(Label(FindAge(focus.Located)+(focus.Sound!=""?" · appui long sur OK pour faire sonner":""),20,76,520,26,16,muted,"Medium"));
  }
  var leg=new View{Position2D=new Position2D(20,mh-62),Size2D=new Size2D(470,42),CornerRadius=new Vector4(14,14,14,14),BackgroundColor=Sf(1,1,1,.92f)};map.Add(leg);
  leg.Add(new View{Position2D=new Position2D(16,9),Size2D=new Size2D(24,24),CornerRadius=new Vector4(12,12,12,12),BackgroundColor=new Color(.94f,.33f,.31f,1),BorderlineWidth=3,BorderlineColor=white});
  leg.Add(Label("Maison",48,6,80,30,16,ink,"Medium"));
  leg.Add(new View{Position2D=new Position2D(134,9),Size2D=new Size2D(24,24),CornerRadius=new Vector4(12,12,12,12),BackgroundColor=white,BorderlineWidth=3,BorderlineColor=accent});
  leg.Add(Label("Appareil (n° de la tuile)",166,6,290,30,16,ink,"Medium"));
  double widthKm=mw*400.0/R/1000.0;
  var sw=Label("Largeur ≈ "+(widthKm<1?((int)(widthKm*1000))+" m":widthKm<10?widthKm.ToString("0.0",frCulture)+" km":Math.Round(widthKm).ToString("N0",frCulture)+" km"),mw-330,mh-62,310,26,15,ink,"Semibold");sw.HorizontalAlignment=HorizontalAlignment.End;map.Add(sw);
  var attr=Label("Plan IGN © IGN · © OpenStreetMap",mw-330,mh-36,310,26,14,dark?new Color(1,1,1,.6f):new Color(0,0,0,.55f),"Medium");attr.HorizontalAlignment=HorizontalAlignment.End;map.Add(attr);
  if(findPan){map.Add(new View{Position2D=new Position2D(cx-1,cy-22),Size2D=new Size2D(3,44),BackgroundColor=accent});map.Add(new View{Position2D=new Position2D(cx-22,cy-1),Size2D=new Size2D(44,3),BackgroundColor=accent});
   var ph=new View{Position2D=new Position2D(mw/2-260,mh-120),Size2D=new Size2D(520,46),CornerRadius=new Vector4(23,23,23,23),BackgroundColor=Sf(1,1,1,.94f)};map.Add(ph);var pt=Label("Flèches : déplacer · CH +/− : zoom · OK : terminer",0,0,520,46,18,ink,"Semibold");pt.HorizontalAlignment=HorizontalAlignment.Center;ph.Add(pt);}
  {int bx=findMapX+mw-86,by=findMapY+(focus!=null?150:20)+2*76;
   var pb=Panel(bx+33,by+33,66,66,findPan?accent:GlassFill(),33);pb.PivotPoint=new Position(.5f,.5f,.5f);pb.PositionUsesPivotPoint=true;
   foreach(var d in new[]{new[]{29,14,8,38},new[]{14,29,38,8}})pb.Add(new View{Position2D=new Position2D(d[0],d[1]),Size2D=new Size2D(d[2],d[3]),CornerRadius=new Vector4(4,4,4,4),BackgroundColor=findPan?white:ink});
   slots.Add(new Slot{Card=pb,Name="Déplacer la carte",Action="findpan",Description=findPan?"Flèches pour déplacer · OK pour terminer":"OK puis flèches pour déplacer la carte",Row=2,X=bx,Y=by,W=66,H=66});}
  for(int z=0;z<2;z++){int bx=findMapX+mw-86,by=findMapY+(focus!=null?150:20)+z*76;
   var zb=Panel(bx+33,by+33,66,66,GlassFill(),33);zb.PivotPoint=new Position(.5f,.5f,.5f);zb.PositionUsesPivotPoint=true;
   var zl=Label(z==0?"+":"−",0,0,66,62,38,ink,"Semibold");zl.HorizontalAlignment=HorizontalAlignment.Center;zb.Add(zl);
   slots.Add(new Slot{Card=zb,Name=z==0?"Zoom avant":"Zoom arrière",Action=z==0?"findzoom:in":"findzoom:out",Description="Zoom de la carte · CH +/− sur la télécommande",Row=z,X=bx,Y=by,W=66,H=66});}
  if(ringAsk!="")BuildRingConfirm();
 }
 string FindPlace(FindItem it)
 {
  if(it.State=="home")return "À la maison";
  string where=it.Dist<1000?"à "+it.Dist+" m":it.Dist<100000?"à "+(it.Dist/1000.0).ToString(it.Dist<10000?"0.0":"0",frCulture)+" km":"à "+(it.Dist/1000).ToString("N0",frCulture)+" km";
  return (it.Zone!=""&&it.State!="not_home"?it.Zone+" · ":"")+where+" de la maison";
 }
 static string FindAge(string iso)
 {
  if(!DateTime.TryParse(iso,CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal,out var t))return "";
  var m=(DateTime.UtcNow-t).TotalMinutes;
  return m<1.5?"localisé à l’instant":m<60?"localisé il y a "+(int)m+" min":m<1440?"localisé il y a "+(int)(m/60)+" h":"localisé le "+t.ToLocalTime().ToString("d MMM",CultureInfo.GetCultureInfo("fr-FR"));
 }
 void BuildPinPad()
 {
  bool create=!PinExists();
  Text(create?(pinFirst==null?"Choisissez un code à 4 chiffres":"Confirmez le code"):"Code requis",0,250,1920,52,38,ink,"Semibold").HorizontalAlignment=HorizontalAlignment.Center;
  Text(create?"Il protégera la page Localiser.":"La page Localiser est protégée.",0,306,1920,32,21,muted,"Medium").HorizontalAlignment=HorizontalAlignment.Center;
  for(int i=0;i<4;i++){bool f=i<pinEntry.Length;Add(new View{Position2D=new Position2D(960-92+i*48,362),Size2D=new Size2D(28,28),CornerRadius=new Vector4(14,14,14,14),BackgroundColor=f?accent:new Color(0,0,0,0),BorderlineWidth=3,BorderlineColor=f?accent:muted});}
  if(pinMsg!="")Text(pinMsg,0,400,1920,32,21,new Color(.89f,.22f,.17f,1),"Semibold").HorizontalAlignment=HorizontalAlignment.Center;
  string[] keys={"1","2","3","4","5","6","7","8","9","del","0","back"};int kw=150,kh=96,g=16,x0=960-(3*kw+2*g)/2,y0=448;
  for(int i=0;i<12;i++){int col=i%3,row=i/3,x=x0+col*(kw+g),y=y0+row*(kh+g);
   var card=Panel(x+kw/2,y+kh/2,kw,kh,GlassFill(),22);card.PivotPoint=new Position(.5f,.5f,.5f);card.PositionUsesPivotPoint=true;
   string lab=keys[i]=="del"?"Effacer":keys[i]=="back"?"Annuler":keys[i];
   var l=Label(lab,0,0,kw,kh,keys[i].Length==1?40:22,ink,keys[i].Length==1?"Display":"Semibold");l.HorizontalAlignment=HorizontalAlignment.Center;card.Add(l);
   slots.Add(new Slot{Card=card,Name="",Action="pin:"+keys[i],Description="Chiffres de la télécommande ou flèches + OK",Row=row,X=x,Y=y,W=kw,H=kh});}
 }
 void ReceiveFind(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){var list=new List<FindItem>();var root=doc.RootElement;
   if(root.ValueKind==JsonValueKind.Object&&root.TryGetProperty("home",out var hw)&&hw.GetArrayLength()==2){homeWx=hw[0].GetDouble();homeWy=hw[1].GetDouble();}
   var arr=root.ValueKind==JsonValueKind.Object?root.GetProperty("items"):root;
   foreach(var d in arr.EnumerateArray())list.Add(new FindItem{Wx=JNum(d,"wx"),Wy=JNum(d,"wy"),Id=JStr(d,"id"),Name=JStr(d,"name"),Bs=JStr(d,"bs"),Model=JStr(d,"model"),State=JStr(d,"state"),Zone=JStr(d,"zone"),Sound=JStr(d,"sound"),Located=JStr(d,"located"),Bat=(int)JNum(d,"bat"),Acc=(int)JNum(d,"acc"),Dx=(int)JNum(d,"dx"),Dy=(int)JNum(d,"dy"),Dist=(long)JNum(d,"dist")});
   findItems.Clear();findItems.AddRange(list);findLoaded=true;}}catch{}
  if(page==11&&findUnlocked&&editing=="")RebuildKeepFocus();
 }
 // Ring confirmation: Annuler is selected first, so a stray OK never rings.
 void HandleRingKey(string k)
 {
  if(k=="Left"||k=="Right"){ringSel=1-ringSel;RebuildKeepFocus();return;}
  if(k=="Return"||k=="KP_Enter"){string id=ringAsk;ringAsk="";RebuildKeepFocus();if(ringSel==1)FindSound(id);return;}
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){ringAsk="";RebuildKeepFocus();}
 }
 void BuildRingConfirm()
 {
  var it=findItems.FirstOrDefault(f=>f.Id==ringAsk);if(it==null){ringAsk="";return;}
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.05f,.07f,.12f,.6f)});
  int pw=760,ph=300,px=(1920-pw)/2,py=(1080-ph)/2;
  var panel=Panel(px,py,pw,ph,Sf(1,1,1,.98f),28);
  var t=Label("Faire sonner « "+Clip(it.Name,26)+" » ?",0,40,pw,46,30,ink,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(t);
  var st=Label(it.Model!=""?it.Model:"",0,92,pw,30,19,muted,"Medium");st.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(st);
  string[] lab={"Annuler","Faire sonner"};int bw=260,bh=84,gx=28,bx=(pw-2*bw-gx)/2;
  for(int i=0;i<2;i++){bool f=i==ringSel;var b=new View{Position2D=new Position2D(bx+i*(bw+gx),160),Size2D=new Size2D(bw,bh),CornerRadius=new Vector4(20,20,20,20),BackgroundColor=f?(i==1?new Color(.89f,.22f,.17f,1):accent):Sf(.93f,.94f,.97f,1)};panel.Add(b);
   var l=Label(lab[i],0,0,bw,bh,24,f?white:ink,"Semibold");l.HorizontalAlignment=HorizontalAlignment.Center;b.Add(l);}
 }
 void FocusFind(string id){findFocus=findFocus==id?"":id;findZoom=-1;panWx=0;panWy=0;findPan=false;RebuildKeepFocus();}
 // One arrow = a quarter of the map, at the current zoom.
 void FindPanStep(string k)
 {
  var focus=findItems.FirstOrDefault(f=>f.Id==findFocus);double R=findZoom>=0?findZooms[findZoom]:(focus!=null?findFocusR:findMapR);
  double cWy=(focus!=null&&focus.Wx>0?focus.Wy:homeWy)+panWy;double lat=Math.Atan(Math.Sinh(Math.PI*(1-2*cWy/256)));double pxPerW=R/400.0*156543.03392*Math.Cos(lat);if(pxPerW<=0)return;
  double step=(k=="Left"||k=="Right"?findMapW:findMapH)/4.0/pxPerW;
  if(k=="Left")panWx-=step;else if(k=="Right")panWx+=step;else if(k=="Up")panWy-=step;else panWy+=step;
  RebuildKeepFocus();
 }
 void FindSound(string id)
 {
  var it=findItems.FirstOrDefault(f=>f.Id==id);if(it==null)return;
  if(it.Sound==""){notice.Text="Cet appareil ne peut pas sonner.";return;}

  IntPtr ac;int r=app_control_create(out ac);
  if(r==0){try{
   r=app_control_set_app_id(ac,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(ac,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(ac,"quiet_ha_calls","[{\"p\":\"button/press\",\"b\":{\"entity_id\":\""+it.Sound+"\"}}]");
   if(r==0)r=app_control_send_launch_request(ac,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(ac);}}
  notice.Text=r==0?"Sonnerie envoyée à « "+it.Name+" »":"Commande indisponible.";
 }
 void ReceiveMobility(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){
   var r=doc.RootElement;var list=new List<BikeStation>();
   foreach(var v in r.GetProperty("Velib").EnumerateArray())list.Add(new BikeStation{Name=JStr(v,"Name"),Dist=(int)JNum(v,"Dist"),Mech=(int)JNum(v,"Mech"),Ebike=(int)JNum(v,"Ebike"),Docks=(int)JNum(v,"Docks"),Dx=(int)JNum(v,"Dx"),Dy=(int)JNum(v,"Dy"),Open=!v.TryGetProperty("Open",out var o)||o.ValueKind!=JsonValueKind.False});
   if(list.Count>0){velib.Clear();velib.AddRange(list);}
   if(r.TryGetProperty("Lime",out var l)){limeCount=(int)JNum(l,"Count");limeNear=(int)JNum(l,"Near");limePts.Clear();if(l.TryGetProperty("Pts",out var pts))foreach(var q in pts.EnumerateArray()){var v=new int[4]{0,0,-1,0};for(int k=0;k<4&&k<q.GetArrayLength();k++)v[k]=q[k].GetInt32();limePts.Add(v);}}
   mobAt=JStr(r,"At");metroDx=(int)JNum(r,"MetroDx");metroDy=(int)JNum(r,"MetroDy");mobLoaded=true;
  }}catch{}
  if(metroFoot!=null)metroFoot.Text=MetroFoot();
  if(page==10&&editing=="")RebuildKeepFocus();
 }
 string MetroFoot()
 {
  if(velib.Count==0)return "OK · 2 prochains passages par direction";
  var st=velib[0];int b=st.Mech+st.Ebike;
  return "Vélib’ "+b+" à "+st.Dist+" m"+(limeCount>=0?" · Lime "+limeCount+" autour":"")+" · onglet Vélos";
 }
 void ReceiveRain(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){var list=new List<KeyValuePair<string,double>>();foreach(var q in doc.RootElement.EnumerateArray())list.Add(new KeyValuePair<string,double>(q[0].GetString()??"",q[1].GetDouble()));if(list.Count>0){rainSlots.Clear();rainSlots.AddRange(list);}}}catch{}
  if(weatherRain!=null){weatherRain.Text=RainText();weatherRain.TextColor=RainColor();ShowRainLine();}
 }
 // Rain in the coming hour, from the 15-minute forecast (first step = the current quarter of an hour).
 string RainText()
 {
  if(rainSlots.Count<5)return "OK · Prévisions du jour et sur 7 jours";
  if(rainSlots[0].Value>=0.1){for(int i=1;i<rainSlots.Count;i++)if(rainSlots[i].Value<0.1)return "Il pleut · fin vers "+rainSlots[i].Key;return "Il pleut · pas d’éclaircie avant 2 h";}
  for(int i=1;i<=4;i++)if(rainSlots[i].Value>=0.1)return "Pluie vers "+rainSlots[i].Key+" · "+rainSlots.Skip(i).Take(4).Sum(x=>x.Value).ToString("0.0",frCulture)+" mm";
  return "Pas de pluie dans l’heure";
 }
 static string OneLine(string t){return (t??"").Replace("\n"," ").Replace("  "," ").Trim();}
 bool RainSoon(){return rainSlots.Count>=5&&rainSlots.Take(5).Any(x=>x.Value>=0.1);}
 // Weather tile: the rain line takes the place of max/min when rain is coming within the hour.
 void ShowRainLine(){if(weatherRain==null||weatherRange==null)return;bool r=RainSoon();weatherRain.Opacity=r?1:0;weatherRange.Opacity=r?0:1;}
 Color RainColor(){return rainSlots.Count>=5&&rainSlots.Take(5).Any(x=>x.Value>=0.1)?new Color(.18f,.50f,.93f,1):muted;}
 // Keeps BelAir's helper listening for banners (Home Assistant messages, Félix, rain) whatever is on screen.
 void SendWatch()
 {
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_watch","1");
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
 }
 void PlayPlex(PlexItem item)
 {
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0)r=app_control_add_extra_data(c,"quiet_plex_play",item.Id+"\t"+item.Type);
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
  notice.Text="Ouverture de « "+item.Title+" » dans Plex…";
  BumpUsage("kIciSQlYEM.plex");Launch("kIciSQlYEM.plex");WriteState("plex_play:"+item.Id+":"+item.Title);
 }
 // ---- Félix page: live map, status, maintenance and Roborock routines ----
 string Fx(string id){return felix.TryGetValue(id,out var v)?v:SensorState(id);}
 static readonly HashSet<string> felixIdle=new HashSet<string>{"","unknown","unavailable","charging","charging_complete","idle","docked","sleeping","paused","error","charger_disconnected"};
 bool FelixBusy(){string v=Fx("vacuum.felix"),s=Fx("sensor.felix_etat");return v=="cleaning"||v=="returning"||!felixIdle.Contains(s);}
 Tizen.NUI.Timer felixTimer;string felixSig="";
 // While the Félix page is open: every 8 s when he is working (the map changes about every 30 s), every minute otherwise.
 void StartFelixTimer()
 {
  if(felixTimer!=null)return;
  felixTimer=new Tizen.NUI.Timer(8000);
  felixTimer.Tick+=(o,a)=>{
   if(page!=9||isPaused){felixTimer.Stop();felixTimer.Dispose();felixTimer=null;return false;}
   bool busy=FelixBusy();if(busy||DateTime.Now-lastFelixRequest>=TimeSpan.FromSeconds(60))RequestExtra(false,false,true,busy);
   return true;};
  felixTimer.Start();
 }
 void FelixSoon(){var t=new Tizen.NUI.Timer(5000);t.Tick+=(o,a)=>{if(!isPaused)RequestExtra(false,false,true);return false;};t.Start();}
 void ReceiveFelix(string json)
 {
  try{using(var doc=JsonDocument.Parse(json)){
   var root=doc.RootElement;felixMap=JStr(root,"Map");felixStamp=JStr(root,"Stamp");
   if(root.TryGetProperty("States",out var st)&&st.ValueKind==JsonValueKind.Object){felix.Clear();foreach(var p in st.EnumerateObject())felix[p.Name]=p.Value.ValueKind==JsonValueKind.String?p.Value.GetString()??"":"";}
   if(root.TryGetProperty("Rooms",out var rm)&&rm.ValueKind==JsonValueKind.Array&&rm.GetArrayLength()>0){felixRooms.Clear();foreach(var r in rm.EnumerateArray())if(r.GetArrayLength()==2)felixRooms.Add(new[]{r[0].GetString()??"",r[1].GetString()??""});}
   if(root.TryGetProperty("Buttons",out var bt)&&bt.ValueKind==JsonValueKind.Array){felixButtons.Clear();foreach(var b in bt.EnumerateArray())if(b.GetArrayLength()==2)felixButtons.Add(new[]{b[0].GetString()??"",b[1].GetString()??""});}
   felixLoaded=true;
  }}catch{}
  var sig=new System.Text.StringBuilder(felixMap).Append('|').Append(felixRooms.Count);foreach(var kv in felix)if(!kv.Key.StartsWith("sensor.felix_temps_restant")&&kv.Key!="sensor.felix_dock_duree_restante_avant_nettoyage_de_la_crepine")sig.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
  bool changed=sig.ToString()!=felixSig;felixSig=sig.ToString();
  if(page==9&&editing==""&&changed)BuildPage();
  WriteState("felix_received:"+felix.Count+":"+felixRooms.Count);
 }
 static int[] ImageSize(string path)
 {
  try{using(var f=File.OpenRead(path)){var b=new byte[64*1024];int n=f.Read(b,0,b.Length);
   if(n>24&&b[0]==0x89&&b[1]==0x50)return new[]{(b[16]<<24)|(b[17]<<16)|(b[18]<<8)|b[19],(b[20]<<24)|(b[21]<<16)|(b[22]<<8)|b[23]};
   for(int i=2;i+9<n;){if(b[i]!=0xFF)break;int m=b[i+1],len=(b[i+2]<<8)|b[i+3];if(m==0xC0||m==0xC2)return new[]{(b[i+7]<<8)|b[i+8],(b[i+5]<<8)|b[i+6]};i+=2+len;}
  }}catch{}
  return new[]{0,0};
 }
 string WhenText(string iso)
 {
  DateTime t;if(!DateTime.TryParse(iso,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out t))return "";
  t=t.ToLocalTime();string day=t.Date==DateTime.Today?"aujourd’hui":t.Date==DateTime.Today.AddDays(-1)?"hier":frDays[(int)t.DayOfWeek]+" "+t.Day;
  return day+" à "+t.ToString("HH:mm");
 }
 static string NumFr(string v,string format){double d;return double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out d)?d.ToString(format,frCulture):"—";}
 void BuildFelix()
 {
  if(!felixLoaded&&DateTime.Now-lastFelixRequest>=TimeSpan.FromSeconds(10))RequestExtra(false,false,true);
  StartFelixTimer();
  int mx=80,my=250,mw=1124,mh=636;
  var map=Panel(mx,my,mw,mh,new Color(.11f,.13f,.18f,1),24);
  if(felixMap!=""&&File.Exists(felixMap)){
   var size=ImageSize(felixMap);int iw=size[0]>0?size[0]:1152,ih=size[1]>0?size[1]:840;
   double s=Math.Min((mw-40)/(double)iw,(mh-80)/(double)ih);int w=(int)(iw*s),h=(int)(ih*s);
   map.Add(new ImageView{ResourceUrl=felixMap,Position2D=new Position2D((mw-w)/2,56+(mh-80-h)/2),Size2D=new Size2D(w,h)});
  }else{var l=Label(felixLoaded?"Carte indisponible":"Chargement de la carte…",0,0,mw,mh,28,new Color(1,1,1,.5f),"Medium");l.HorizontalAlignment=HorizontalAlignment.Center;map.Add(l);}
  string room=Fx("sensor.felix_piece_actuelle");
  map.Add(Label("CARTE"+(FelixBusy()&&room!=""&&room!="unknown"?" · "+room.ToUpperInvariant():""),24,16,700,30,17,new Color(1,1,1,.7f),"Semibold"));
  var upd=Label(felixStamp!=""?"Mise à jour "+WhenText(felixStamp):"",mw-524,16,500,30,16,new Color(1,1,1,.55f),"Medium");upd.HorizontalAlignment=HorizontalAlignment.End;map.Add(upd);
  int rx=1236,rw=604;
  var status=Panel(rx,my,rw,204,GlassFill(),24);
  string vac=Fx("vacuum.felix"),etat=Fx("sensor.felix_etat");if(etat==""||etat=="unknown")etat=vac;
  status.Add(Label("ÉTAT",26,16,300,24,15,muted,"Semibold"));
  status.Add(Label(FelixText(etat),26,40,560,52,38,ink,"Display"));
  int battery;int.TryParse(Fx("sensor.felix_batterie"),out battery);
  var track=new View{Position2D=new Position2D(26,104),Size2D=new Size2D(470,10),BackgroundColor=Sf(.88f,.9f,.94f,1),CornerRadius=new Vector4(5,5,5,5)};status.Add(track);
  track.Add(new View{Size2D=new Size2D(470*Math.Max(0,Math.Min(100,battery))/100,10),BackgroundColor=battery<20?new Color(.85f,.25f,.2f,1):accent,CornerRadius=new Vector4(5,5,5,5)});
  var pct=Label(battery+" %",506,92,72,34,20,ink,"Semibold");pct.HorizontalAlignment=HorizontalAlignment.End;status.Add(pct);
  string line;
  if(FelixBusy())line="Nettoyage "+Fx("sensor.felix_progression_du_nettoyage")+" %"+(room!=""&&room!="unknown"?" · "+room:"");
  else{string end=WhenText(Fx("sensor.felix_fin_du_dernier_nettoyage"));line=end==""?"Aucun nettoyage récent":"Dernier nettoyage "+end+" · "+NumFr(Fx("sensor.felix_surface_de_nettoyage"),"0.#")+" m² en "+NumFr(Fx("sensor.felix_duree_de_nettoyage"),"0")+" min";}
  var ll=Label(line,26,134,556,52,19,muted,"Medium");ll.MultiLine=true;ll.LineWrapMode=LineWrapMode.Word;status.Add(ll);
  string[] labels={"Nettoyer","Pause","Base"},services={"custom","pause","return_to_base"},descs={"Choisir les pièces et aspirer, laver ou les deux","Mettre en pause","Renvoyer Félix sur sa base"};
  int bw=(rw-2*14)/3;
  for(int i=0;i<3;i++){
   int x=rx+i*(bw+14),y=474;
   var b=Panel(x+bw/2,y+36,bw,72,i==0?accent:GlassFill(),18);b.PivotPoint=new Position(.5f,.5f,.5f);b.PositionUsesPivotPoint=true;
   var t=Label(labels[i],0,0,bw,72,23,i==0?white:ink,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;b.Add(t);
   slots.Add(new Slot{Card=b,Name="Félix · "+labels[i],Action=i==0?"felix_clean":"ha_vac:"+services[i],Description=descs[i],Row=0,X=x,Y=y,W=bw,H=72});
  }
  var care=Panel(rx,566,rw,320,GlassFill(),24);
  care.Add(Label("ENTRETIEN",26,16,300,24,15,muted,"Semibold"));
  string[][] parts={new[]{"sensor.felix_temps_restant_brosse_principale","Brosse principale","300"},new[]{"sensor.felix_temps_restant_brosse_laterale","Brosse latérale","200"},new[]{"sensor.felix_temps_restant_filtre","Filtre","150"},new[]{"sensor.felix_temps_restant_capteurs","Capteurs","30"}};
  for(int i=0;i<parts.Length;i++){
   double left;bool ok=double.TryParse(Fx(parts[i][0]),NumberStyles.Float,CultureInfo.InvariantCulture,out left);double max=double.Parse(parts[i][2],CultureInfo.InvariantCulture);
   int y=50+i*52;double ratio=ok?Math.Max(0,Math.Min(1,left/max)):0;
   care.Add(Label(parts[i][1],26,y,260,30,20,ink,"Semibold"));
   var v=Label(ok?Math.Round(left).ToString(CultureInfo.InvariantCulture)+" h":"—",440,y,138,30,19,ratio<.1?new Color(.85f,.25f,.2f,1):muted,"Semibold");v.HorizontalAlignment=HorizontalAlignment.End;care.Add(v);
   var tr=new View{Position2D=new Position2D(26,y+34),Size2D=new Size2D(552,6),BackgroundColor=Sf(.88f,.9f,.94f,1),CornerRadius=new Vector4(3,3,3,3)};care.Add(tr);
   tr.Add(new View{Size2D=new Size2D((int)(552*ratio),6),BackgroundColor=ratio<.1?new Color(.85f,.25f,.2f,1):new Color(accent.R,accent.G,accent.B,.8f),CornerRadius=new Vector4(3,3,3,3)});
  }
  var issues=new List<string>();
  if(Fx("binary_sensor.felix_penurie_d_eau")=="on")issues.Add("Pénurie d’eau");
  if(Fx("binary_sensor.felix_dock_reservoir_d_eau_propre")=="on")issues.Add("Réservoir d’eau propre à remplir");
  if(Fx("binary_sensor.felix_dock_reservoir_d_eau_sale")=="on")issues.Add("Réservoir d’eau sale à vider");
  string err=Fx("sensor.felix_erreur_de_l_aspirateur"),derr=Fx("sensor.felix_dock_erreur_de_dock");
  if(err!=""&&err!="none"&&err!="unknown")issues.Add("Erreur : "+err.Replace('_',' '));
  if(derr!=""&&derr!="ok"&&derr!="unknown")issues.Add("Station : "+derr.Replace('_',' '));
  var dock=Label(issues.Count==0?"Station et réservoirs OK":string.Join(" · ",issues),26,264,556,40,19,issues.Count==0?muted:new Color(.85f,.25f,.2f,1),"Semibold");dock.MultiLine=true;care.Add(dock);
  int count=Math.Min(felixButtons.Count,7);
  if(count>0){
   int gap=14,cwid=(1760-(count-1)*gap)/count;
   for(int i=0;i<count;i++){
    int x=80+i*(cwid+gap),y=908;
    var chip=Panel(x+cwid/2,y+31,cwid,62,GlassFill(),31);chip.PivotPoint=new Position(.5f,.5f,.5f);chip.PositionUsesPivotPoint=true;
    var t=Label(felixButtons[i][1],10,0,cwid-20,62,19,ink,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;chip.Add(t);
    slots.Add(new Slot{Card=chip,Name=felixButtons[i][1],Action="ha_btn:"+felixButtons[i][0],Description="Programme Roborock · OK pour lancer",Row=1,X=x,Y=y,W=cwid,H=62});
   }
  }
  if(cleanOpen)BuildCleanPopup();
 }
 // ---- Félix custom clean: rooms (none = whole flat) and vacuum / vacuum + mop / mop ----
 int CleanRowLength(int row){return row==0?felixRooms.Count+1:row==1?3:2;}
 void BuildCleanPopup()
 {
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(.05f,.07f,.12f,.6f)});
  int pw=1440,ph=620,px=(1920-pw)/2,py=(1080-ph)/2;
  var panel=Panel(px,py,pw,ph,Sf(1,1,1,.98f),28);
  panel.Add(Label("Nettoyage sur mesure",48,30,900,46,34,ink,"Semibold"));
  panel.Add(Label(cleanRooms.Count==0?"Tout l’appartement":cleanRooms.Count+" pièce"+(cleanRooms.Count>1?"s":"")+" · "+cleanModeNames[cleanMode].ToLowerInvariant(),48,78,900,30,21,muted,"Medium"));
  panel.Add(Label("PIÈCES",48,132,400,24,16,muted,"Semibold"));
  int n=felixRooms.Count+1,gap=12,cw=(pw-96-(n-1)*gap)/n;
  for(int i=0;i<n;i++){
   string name=i==0?"Toutes":felixRooms[i-1][1];bool on=i==0?cleanRooms.Count==0:cleanRooms.Contains(felixRooms[i-1][0]);bool f=cleanRow==0&&cleanCol==i;
   var chip=new View{Position2D=new Position2D(48+i*(cw+gap),164),Size2D=new Size2D(cw,72),CornerRadius=new Vector4(20,20,20,20),BackgroundColor=on?accent:Sf(.93f,.94f,.97f,1),BorderlineWidth=f?4:0,BorderlineColor=on?ink:accent};panel.Add(chip);
   var t=Label(name,8,0,cw-16,72,name.Length>12?17:20,on?white:ink,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;t.MultiLine=true;t.LineWrapMode=LineWrapMode.Word;chip.Add(t);
  }
  if(felixRooms.Count==0)panel.Add(Label("Chargement des pièces…",48+cw+gap,164,700,72,20,muted,"Medium"));
  panel.Add(Label("MODE",48,268,400,24,16,muted,"Semibold"));
  int mw=(pw-96-2*gap)/3;
  for(int i=0;i<3;i++){
   bool on=cleanMode==i,f=cleanRow==1&&cleanCol==i;
   var m=new View{Position2D=new Position2D(48+i*(mw+gap),300),Size2D=new Size2D(mw,80),CornerRadius=new Vector4(20,20,20,20),BackgroundColor=on?accent:Sf(.93f,.94f,.97f,1),BorderlineWidth=f?4:0,BorderlineColor=on?ink:accent};panel.Add(m);
   var t=Label(cleanModeNames[i],0,0,mw,80,24,on?white:ink,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;m.Add(t);
  }
  string[] acts={"Lancer","Annuler"};int[] aw={340,240};int ax=(pw-aw[0]-aw[1]-24)/2;
  for(int i=0;i<2;i++){
   bool f=cleanRow==2&&cleanCol==i;
   var b=new View{Position2D=new Position2D(ax,430),Size2D=new Size2D(aw[i],84),CornerRadius=new Vector4(42,42,42,42),BackgroundColor=i==0?accent:Sf(.93f,.94f,.97f,1),BorderlineWidth=f?5:0,BorderlineColor=i==0?ink:accent};panel.Add(b);
   var t=Label(acts[i],0,0,aw[i],84,26,i==0?white:ink,"Semibold");t.HorizontalAlignment=HorizontalAlignment.Center;b.Add(t);
   ax+=aw[i]+24;
  }
  var hint=Label("↑/↓ ligne · ←/→ choisir · OK cocher ou valider · Retour fermer",0,ph-58,pw,30,19,muted,"Medium");hint.HorizontalAlignment=HorizontalAlignment.Center;panel.Add(hint);
 }
 void HandleCleanKey(string k)
 {
  if(k=="XF86Back"||k=="Back"||k=="Escape"||k=="XF86Exit"){cleanOpen=false;BuildPage();return;}
  if(k=="Up"||k=="Down"){cleanRow=Math.Max(0,Math.Min(2,cleanRow+(k=="Up"?-1:1)));cleanCol=cleanRow==1?cleanMode:Math.Min(cleanCol,CleanRowLength(cleanRow)-1);BuildPage();return;}
  if(k=="Left"||k=="Right"){int len=CleanRowLength(cleanRow);cleanCol=(cleanCol+(k=="Left"?-1:1)+len)%len;BuildPage();return;}
  if(k!="Return"&&k!="KP_Enter")return;
  if(cleanRow==0){if(cleanCol==0)cleanRooms.Clear();else{string id=felixRooms[cleanCol-1][0];if(!cleanRooms.Remove(id))cleanRooms.Add(id);}BuildPage();return;}
  if(cleanRow==1){cleanMode=cleanCol;BuildPage();return;}
  if(cleanCol==1){cleanOpen=false;BuildPage();return;}
  StartClean();
 }
 void StartClean()
 {
  var ids=new List<string>();var names=new List<string>();
  foreach(var r in felixRooms)if(cleanRooms.Contains(r[0])){int v;if(int.TryParse(r[0],out v)){ids.Add(v.ToString(CultureInfo.InvariantCulture));names.Add(r[1]);}}
  string calls="[{\"p\":\"select/select_option\",\"b\":{\"entity_id\":\"select.felix_mode_de_nettoyage\",\"option\":\""+cleanModeIds[cleanMode]+"\"}},"
   +(ids.Count>0?"{\"p\":\"vacuum/send_command\",\"b\":{\"entity_id\":\"vacuum.felix\",\"command\":\"app_segment_clean\",\"params\":[{\"segments\":["+string.Join(",",ids)+"],\"repeat\":1}]}}]"
   :"{\"p\":\"vacuum/start\",\"b\":{\"entity_id\":\"vacuum.felix\"}}]");
  IntPtr c;int r2=app_control_create(out c);
  if(r2==0){try{
   r2=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r2==0)r2=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r2==0)r2=app_control_add_extra_data(c,"quiet_ha_calls",calls);
   if(r2==0)r2=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
  cleanOpen=false;BuildPage();
  notice.Text=r2!=0?"Commande maison indisponible.":"Félix · "+cleanModeNames[cleanMode].ToLowerInvariant()+" · "+(names.Count==0?"tout l’appartement":string.Join(", ",names));
  FelixSoon();WriteState("felix_clean:"+cleanModeIds[cleanMode]+":"+string.Join("+",ids));
 }
 // ---- morning brief: shown once a day on the first visible moment between 5:00 and 11:00, closed by any key ----
 bool MaybeBrief()
 {
  int h=DateTime.Now.Hour;string today=DateTime.Today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
  if(!prefBrief||isPaused||briefRoot!=null||ambientRoot!=null||tabs==null||h<5||h>=11||briefDate==today)return false;
  OpenBrief();return true;
 }
 void OpenBrief()
 {
  if(briefRoot!=null)return;
  CloseAmbient();
  briefRoot=new View{Size2D=new Size2D(1920,1080),BackgroundColor=dark?new Color(.05f,.06f,.09f,.985f):new Color(.96f,.96f,.97f,.985f)};
  string glow=AuroraWallFile();if(glow!="")briefRoot.Add(new ImageView{ResourceUrl=glow,Size2D=new Size2D(1920,1080),Opacity=.9f});
  var now=DateTime.Now;
  briefRoot.Add(Label("BRIEF DU MATIN",120,170,700,36,24,accent,"Semibold"));
  briefRoot.Add(Label(now.Hour<12?"Bonjour":"Bonne journée",112,208,760,130,104,ink,"Display"));
  briefRoot.Add(Label(frDays[(int)now.DayOfWeek]+" "+now.Day+" "+frMonths[now.Month-1],120,340,700,56,40,muted,"Medium"));
  briefClock=Label(now.ToString("HH:mm"),112,420,700,180,150,ink,"Display");briefRoot.Add(briefClock);
  briefRoot.Add(Label("Appuyez sur une touche pour continuer",120,940,700,34,22,muted,"Medium"));
  briefCards.Clear();
  string[] keys={"weather","metro","home","energy","felix","plex"};string[] caps={"MÉTÉO · PARIS 12E","MÉTRO 6 · BEL-AIR","À LA MAISON","ÉLECTRICITÉ · HIER","FÉLIX","À REPRENDRE"};
  for(int i=0;i<keys.Length;i++){
   int x=860+(i%2)*500,y=150+(i/2)*262;
   var card=new View{Position2D=new Position2D(x,y),Size2D=new Size2D(476,238),BackgroundColor=Sf(1,1,1,.9f),CornerRadius=new Vector4(26,26,26,26)};briefRoot.Add(card);
   card.Add(Label(caps[i],28,22,420,26,17,muted,"Semibold"));
   var big=Label("…",28,56,428,96,38,ink,"Display");big.MultiLine=true;big.LineWrapMode=LineWrapMode.Word;big.VerticalAlignment=VerticalAlignment.Top;card.Add(big);
   var small=Label("",28,156,428,64,20,muted,"Medium");small.MultiLine=true;small.LineWrapMode=LineWrapMode.Word;small.VerticalAlignment=VerticalAlignment.Top;card.Add(small);
   briefCards[keys[i]]=new[]{big,small};
  }
  Window.Default.Add(briefRoot);briefRoot.RaiseToTop();UpdateBrief();
  if(briefTimer!=null){briefTimer.Stop();briefTimer.Dispose();}
  briefTimer=new Tizen.NUI.Timer(180000);briefTimer.Tick+=(o,a)=>{CloseBrief(false);return false;};briefTimer.Start();
  RequestHomeData();RequestEnergy();RequestExtra(true,true,false);lastSensorRequest=DateTime.Now;RequestHaStates();
  WriteState("brief_opened");
 }
 void SetBrief(string key,string big,string small){if(briefCards.TryGetValue(key,out var l)){l[0].Text=big;l[1].Text=small;}}
 void UpdateBrief()
 {
  if(briefRoot==null)return;
  string rain="Pas de pluie prévue aujourd’hui";
  foreach(var h in forecastHours){if(h.Day!=""&&h.Day!=DateTime.Today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture))break;int p;if(int.TryParse(h.Precipitation.TrimEnd('%'),out p)&&p>=40){rain="Pluie probable vers "+h.Time+" ("+p+" %)";break;}}
  SetBrief("weather",forecast.Count>0||weatherTemperatureText!="--°"?weatherTemperatureText+" · "+weatherConditionText:"…",(weatherHigh!=""?"Max "+weatherHigh+" · Min "+weatherLow+"\n":"")+rain);
  SetBrief("metro",metroAvailable?"Nation "+ShortNext("Nation")+"\nÉtoile "+ShortNext("Etoile"):"…",!trafficAvailable?"":trafficHasAlert?"Trafic perturbé : "+trafficHeader:"Trafic normal sur la ligne 6");
  SetBrief("home","Salon "+SensorNum("sensor.salon_sensor_temperature","0.0")+"°\nChambre "+SensorNum("sensor.chambre_temperature_temperature","0.0")+"°","Humidité "+SensorNum("sensor.salon_sensor_humidite","0")+" % et "+SensorNum("sensor.chambre_temperature_humidite","0")+" %");
  if(energyDays.Count>0){var last=energyDays[energyDays.Count-1];double avg=0;foreach(var d in energyDays)avg+=d.Kwh;avg/=energyDays.Count;double vs=avg>0?(last.Kwh-avg)/avg*100:0;
   SetBrief("energy",Kwh(last.Kwh),Eur(last.Eur)+" · "+(vs>=0?"+":"−")+Math.Abs(vs).ToString("0",frCulture)+" % par rapport à la moyenne sur 30 jours");}
  string fs=Fx("sensor.felix_etat");if(fs==""||fs=="unknown")fs=Fx("vacuum.felix");string fb=Fx("sensor.felix_batterie");string fe=WhenText(Fx("sensor.felix_fin_du_dernier_nettoyage"));
  SetBrief("felix",FelixText(fs)+(fb!=""&&fb!="unknown"?" · "+fb+" %":""),fe!=""?"Dernier nettoyage "+fe:"");
  if(plexItems.Count>0)SetBrief("plex",plexItems[0].Title,plexItems[0].Sub+(plexItems.Count>1?"\npuis "+plexItems[1].Title:""));
  else SetBrief("plex",mediaLoaded?"Rien en cours":"…","");
 }
 void CloseBrief(bool seen)
 {
  if(briefRoot==null)return;
  if(briefTimer!=null){briefTimer.Stop();briefTimer.Dispose();briefTimer=null;}
  Window.Default.Remove(briefRoot);briefRoot.Dispose();briefRoot=null;briefClock=null;briefCards.Clear();lastInput=DateTime.Now;
  if(seen){briefDate=DateTime.Today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);SavePrefs();}
  WriteState("brief_closed:"+seen);
 }
 void RequestWeather()
 {
  lastWeatherRequest=DateTime.Now;SendDataRequest(true,false,false,false);
 }
 void RequestMetro()
 {
  lastMetroRequest=DateTime.Now;SendDataRequest(false,true,false,false);
 }
 void RequestTraffic()
 {
  lastTrafficRequest=DateTime.Now;lastLinesRequest=DateTime.Now;SendDataRequest(false,false,true,false,true);
 }
 void RequestNews()
 {
  lastNewsRequest=DateTime.Now;SendDataRequest(false,false,false,true);
 }
 void RequestHomeData()
 {
  lastWeatherRequest=DateTime.Now;lastMetroRequest=DateTime.Now;lastTrafficRequest=DateTime.Now;lastNewsRequest=DateTime.Now;lastLinesRequest=DateTime.Now;lastMarketsRequest=DateTime.Now;SendDataRequest(true,true,true,true,true,true);
 }
 void SendDataRequest(bool weather,bool metro,bool traffic,bool news,bool lines=false,bool markets=false,bool energy=false)
 {
  if(isPaused)return;
  IntPtr c;int r=app_control_create(out c);
  if(r==0){try{
   r=app_control_set_app_id(c,"com.samsung.tv.quicklaunch");
   if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0&&weather)r=app_control_add_extra_data(c,"quiet_weather_request","1");
   if(r==0&&metro)r=app_control_add_extra_data(c,"quiet_metro_request","1");
   if(r==0&&traffic)r=app_control_add_extra_data(c,"quiet_traffic_request","1");
   if(r==0&&news)r=app_control_add_extra_data(c,"quiet_news_request","1");
   if(r==0&&lines)r=app_control_add_extra_data(c,"quiet_lines_request","1");
   if(r==0&&markets)r=app_control_add_extra_data(c,"quiet_markets_request","1");
   if(r==0&&energy)r=app_control_add_extra_data(c,"quiet_energy_request","1");
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);
  }finally{app_control_destroy(c);}}
  if(r!=0){
   if(weather){weatherText="Paris 12e · météo indisponible";weatherConditionText="Indisponible";weatherTemperatureText="--°";weatherHigh="";weatherLow="";if(weatherCondition!=null)weatherCondition.Text=OneLine(weatherConditionText);if(weatherTemperature!=null)weatherTemperature.Text=weatherTemperatureText;if(weatherRange!=null)weatherRange.Text=WeatherRange();DrawWeatherIcon();}
   if(metro){metroAvailable=false;metroDepartures.Clear();UpdateMetroWidget();if(page==4&&!lineDetailOpen)BuildPage();}
   if(traffic){trafficAvailable=false;trafficHasAlert=false;UpdateMetroWidget();if(page==4)BuildPage();}
   if(news&&newsSections.Count==0&&newsTopTitle!=null)newsTopTitle.Text="ACTUALITÉS   ⌄";
   WriteState("data_request_failed:"+r);
  }
 }
 void Launch(string id,string menuItem=null)
 {
  if((id=="com.samsung.tv.store"||id=="com.samsung.tv.csfs")&&LaunchSystemApp(id))return;
  IntPtr c;int r=app_control_create(out c);if(r!=0){ShowError("Impossible d’ouvrir l’application.");if(id=="kIciSQlYEM.plex")Launch("com.samsung.tv.store");return;}
  try{r=app_control_set_app_id(c,id);if(r==0)r=app_control_set_operation(c,"http://tizen.org/appcontrol/operation/default");
   if(r==0&&id=="org.tizen.menu"){
    if(menuItem!=null){
     r=app_control_add_extra_data(c,"menuitem",menuItem);
     if(r==0)r=app_control_add_extra_data(c,"launchoption","pressed");
    }
    if(r==0)r=app_control_add_extra_data(c,"__K_EX_FORCE_BOOST","enable");
    if(r==0)r=app_control_add_extra_data(c,"__K_SPLASH_SCREEN","LA_UI_VR_TRANSPARENT");
   }
   if(r==0)r=app_control_send_launch_request(c,IntPtr.Zero,IntPtr.Zero);WriteState("launch:"+id+(menuItem==null?"":":"+menuItem)+":"+r);
  }finally{app_control_destroy(c);}
  if(r!=0){
   if(id=="kIciSQlYEM.plex"){
    ShowError("Plex n’est pas installé. Recherchez-le dans Applis Samsung.");
    Launch("com.samsung.tv.store");
   }else ShowError("Impossible d’ouvrir cet écran. Essayez les paramètres TV.");
  }
 }
 void ShowError(string text){notice.Text=text;}
 int NewsArticleCount(){int count=0;foreach(var section in newsSections)count+=section.Items.Count;return count;}
 void WriteState(string activity){try{File.WriteAllText(StateFile,DateTime.UtcNow.ToString("o")+"\npage="+page+"\nselected="+selected+"\narea="+area+"\nactivity="+activity+"\nkey="+lastKey+"\nfont_loaded="+fontLoaded+"\nstyle=free-my-tv\nbuild_ms="+buildMilliseconds+"\nlibrary_page="+libraryPage+"\nlibrary_count="+library.Count+"\nlibrary_error="+libraryError+"\nselected_name="+(selected>=0&&selected<slots.Count?slots[selected].Name:"")+"\nartwork_cached="+artworkCache.Count+"\nartwork_ready="+ArtworkReadyCount()+"\nartwork_pending="+ArtworkPending()+"\nartwork_views_created="+artworkViewsCreated+"\npage_artwork_cold="+pageArtworkCold+"\nprocess_id="+Process.GetCurrentProcess().Id+"\nediting="+editing+"\nvolume="+currentVolume+"\nmute="+currentMute+"\nbrightness="+SettingValue("brightness")+"\ngame-mode="+SettingValue("game-mode")+"\nsleeptimer="+SettingValue("sleeptimer")+"\nweather="+weatherText+"\nforecast_count="+forecast.Count+"\nforecast_hours="+forecastHours.Count+"\nforecast_hour_offset="+forecastHourOffset+"\nforecast_hour_focus="+forecastHourFocus+"\nforecast_day_focus="+forecastDayFocus+"\nforecast_detail_open="+forecastDetailOpen+"\nselected_forecast_hour="+selectedForecastHour+"\nselected_forecast_day="+selectedForecastDay+"\nmetro_available="+metroAvailable+"\nmetro_departures="+metroDepartures.Count+"\nmetro_updated="+metroUpdated+"\ntraffic_available="+trafficAvailable+"\ntraffic_alert="+trafficHasAlert+"\ntraffic_updated="+trafficUpdated+"\nnews_articles="+NewsArticleCount()+"\nnews_updated="+newsUpdated+"\nnews_menu_open="+newsMenuOpen+"\nfact_index="+factIndex+"\n");}catch{}}
 public static void Main(string[] args){using(var app=new QuietHome())app.Run(args);}
}
