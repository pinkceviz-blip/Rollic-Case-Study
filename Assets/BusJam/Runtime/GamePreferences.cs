using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Rollic.BusJam
{
 public sealed class GamePreferences:MonoBehaviour
 {
  public static bool English=>PlayerPrefs.GetInt("Rollic.BusJam.Language",0)==1;
  public static bool Dark=>PlayerPrefs.GetInt("Rollic.BusJam.Dark",0)==1;
  public static bool LampsOn=>Dark;
  public static bool Sound=>PlayerPrefs.GetInt("Rollic.BusJam.Sound",1)==1;
  readonly Dictionary<Camera,Color> skies=new Dictionary<Camera,Color>();
  AudioSource music;AudioClip clip;float next;bool? appliedDark,appliedLamps;
  static readonly Dictionary<string,string> words=new Dictionary<string,string>{
   {"DİL: TÜRKÇE","LANGUAGE: ENGLISH"},{"DİL: İNGİLİZCE","LANGUAGE: ENGLISH"},{"SES: AÇIK","SOUND: ON"},{"SES: KAPALI","SOUND: OFF"},{"TEMA: AYDINLIK","THEME: LIGHT"},{"TEMA: KARANLIK","THEME: DARK"},
   {"BÖLÜMÜ YENİDEN BAŞLAT","RESTART LEVEL"},{"NASIL OYNANIR?","HOW TO PLAY?"},{"ANA SAYFAYA DÖN","BACK TO HOME"},{"OYUNA DEVAM ET","CONTINUE GAME"},{"DEVAM ET","CONTINUE"},{"YENİDEN BAŞLAT","RESTART"},{"YENİDEN DENE","TRY AGAIN"},{"SONRAKİ BÖLÜM","NEXT LEVEL"},{"YENİDEN OYNA","PLAY AGAIN"},{"OYNA","PLAY"},
   {"DURAKLATILDI","PAUSED"},{"SÜRE DOLDU!","TIME'S UP!"},{"HARİKA!","GREAT!"},{"Tüm yolcular yola çıktı.","All passengers are on their way."},{"Bir kez daha dene,\nbütün yolcuları otobüse bindir!","Try again,\nget all passengers on board!"},
   {"YOLU AÇ","CLEAR THE WAY"},{"RENGİNİ EŞLEŞTİR","MATCH THE COLOR"},{"OTOBÜSÜ DOLDUR","FILL THE BUS"},{"Yolu açık yolculara dokun.","Tap passengers with a clear path."},{"Otobüsle aynı renkte olanlar biner.\nDiğerleri bekleme alanına geçer.","Matching passengers board the bus.\nOthers move to the waiting area."},{"Süre bitmeden tüm yolcuları bindir.","Board everyone before time runs out."},
   {"Vitrin otobüsünün rengini seç","Choose your display bus color"},{"KIRMIZI","RED"},{"TURKUAZ","TURQUOISE"},{"YEŞİL","GREEN"},{"SARI","YELLOW"},{"MOR","PURPLE"},{"PEMBE","PINK"},{"KALAN HAK","LIVES"},{"DOLU","FULL"},{"REHBER","GUIDE"},{"GARAJ","GARAGE"},{"ANA SAYFA","HOME"},{"BÖLÜMLER","LEVELS"},{"AYARLAR","SETTINGS"},{"BÖLÜM","LEVEL"},
   {"TAMAM","OK"},{"VAZGEÇ","CANCEL"},{"BAŞLAT","START"},{"YENİ HAK","NEW LIFE"},{"HAKLARIN DOLU","LIVES FULL"},{"HAKKIN KALMADI","NO LIVES LEFT"},{"YENİDEN BAŞLAT?","RESTART?"},{"Her 4 saatte 1 hak kazanırsın.\nEn fazla 10 hak birikebilir.","Earn 1 life every 4 hours.\nYou can have up to 10 lives."},{"Mevcut bölüm kaydının yerine\nseçtiğin bölüm başlatılacak.","The selected level will replace\nyour current level save."},{"HATA OLUŞTU","SOMETHING WENT WRONG"},
   {"Yolu açık bir yolcuya dokun","Tap a passenger with a clear route to the road"},{"Bütün yolcular bindi!","ALL ABOARD!"}
  };
  public static string Translate(string value)
  {
   if(string.IsNullOrEmpty(value))return value;
   if(English){if(words.TryGetValue(value,out var en))return en;return value.Replace("BÖLÜM ","LEVEL ").Replace("SÜRE  ","TIME  ").Replace(" bölüm tamamlandı"," levels completed").Replace(" TAMAMLANDI"," COMPLETE").Replace("Kalan hak: ","Lives: ").Replace("Yeni hak için kalan süre:","Next life in:").Replace("Bu işlem 1 hak harcar.","This costs 1 life.");}
   foreach(var pair in words)if(value==pair.Value)return pair.Key;
   return value;
  }
  public static void Set(string key,int value){PlayerPrefs.SetInt("Rollic.BusJam."+key,value);PlayerPrefs.Save();}
  void Awake()
  {
   music=gameObject.AddComponent<AudioSource>();music.loop=true;music.spatialBlend=0;music.volume=.22f;music.playOnAwake=false;
   const int rate=22050;const float beat=.4f;int count=rate*16;var samples=new float[count];int[] melody={72,76,79,76,74,77,81,77,71,74,79,74,72,76,79,83,76,79,84,79,74,77,81,77,71,74,79,74,72,76,79,76,72,79,76,79,74,81,77,81};
   for(int n=0;n<melody.Length;n++){int start=Mathf.RoundToInt(n*beat*rate);float hz=440*Mathf.Pow(2,(melody[n]-69)/12f);for(int i=0;i<rate*.38f&&start+i<count;i++){float t=(float)i/rate;float env=Mathf.Min(1,t/.012f)*Mathf.Exp(-t*10);samples[start+i]+=(Mathf.Sin(2*Mathf.PI*hz*t)+.2f*Mathf.Sin(4*Mathf.PI*hz*t))*env*.24f;}}
   clip=AudioClip.Create("Bus Jam gentle background melody",count,1,rate,false);clip.SetData(samples,0);music.clip=clip;music.Play();Apply();
  }
  void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;Apply();}
  public void Apply()
  {
   AudioListener.volume=Sound?1:0;if(music)music.mute=!Sound;
   Shader.SetGlobalFloat("_BusJamDark",Dark?1:0);
   Shader.SetGlobalFloat("_BusJamLampsOn",LampsOn?1:0);
   foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){if(!skies.ContainsKey(camera))skies[camera]=camera.backgroundColor;camera.backgroundColor=Dark?Color.Lerp(skies[camera],new Color(.025f,.055f,.14f),.9f):skies[camera];if(camera.CompareTag("MainCamera")&&!camera.GetComponent<AudioListener>())camera.gameObject.AddComponent<AudioListener>();}
   if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==0){var listener=Camera.main??UnityEngine.Object.FindAnyObjectByType<Camera>();if(listener)listener.gameObject.AddComponent<AudioListener>();}
   if(appliedDark!=Dark||appliedLamps!=LampsOn){appliedDark=Dark;appliedLamps=LampsOn;foreach(var city in UnityEngine.Object.FindObjectsByType<MenuCityView>(FindObjectsSortMode.None))city.Render();}
   foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
   {
    var label=text.GetComponent<PreferenceLabel>();if(!label)label=text.gameObject.AddComponent<PreferenceLabel>();label.Apply(text);
   }
  }
  void OnDestroy(){Shader.SetGlobalFloat("_BusJamDark",0);Shader.SetGlobalFloat("_BusJamLampsOn",0);if(clip)Destroy(clip);}
 }
 public sealed class PreferenceLabel:MonoBehaviour
 {
  string source,last;public void Apply(Text text){if(text.text!=last)source=text.text;last=GamePreferences.Translate(source);if(text.text!=last)text.text=last;}
 }
 public sealed class EnglishOnlyLabel:MonoBehaviour
 {
  CanvasGroup group;void Awake(){group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;Refresh();}void Update(){Refresh();}void Refresh(){group.alpha=GamePreferences.English?1:0;}
 }

}
