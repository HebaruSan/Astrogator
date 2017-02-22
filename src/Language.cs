using System.Globalization;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;

	public static class Language {

		static Language()
		{
			if (GameDatabase.Instance.ExistsConfigNode(MyLocaleURL)) {
				DbgFmt("Loading current locale");
				LoadLanguage(MyLocaleURL);
			} else {
				DbgFmt("Loading default locale");
				LoadLanguage(DefaultLocaleURL);
			}
		}

		private const string DefaultLocale = "en_US";
		private static string MyLocale =  CultureInfo.CurrentCulture.TwoLetterISOLanguageName + "_" + System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
		private static string DefaultLocaleURL = LanguageURL(DefaultLocale);
		private static string MyLocaleURL = LanguageURL(MyLocale);

		private static string LanguageURL(string locale)
		{
			return FilePath(string.Format("lang/{0}/LANGUAGE", locale));
		}

		private static void LoadLanguage(string url)
		{
			DbgFmt("Loading language from {0}", url);
			ConfigNode langCfg = GameDatabase.Instance.GetConfigNode(url);
			if (langCfg != null) {
				DbgFmt("ConfigNode returned");
				ConfigNode[] translations = langCfg.GetNodes("TRANSLATION");
				DbgFmt("Got translations");
				for (int i = 0; i < translations.Length; ++i) {
					DbgFmt("Checking resource {0}", i);
					string key = translations[i].GetValue("name");
					string val = translations[i].GetValue("string").Replace("<<", "{").Replace(">>","}");
					DbgFmt("Token {0} = {1}", key, val);
					switch (key) {

						case "mainTitle": mainTitle = val; break;
						case "mainTooltip": mainTooltip = val; break;

						case "normalSubtitle": normalSubtitle = val; break;
						case "inboundHyperbolicWarning": inboundHyperbolicWarning = val; break;
						case "outboundHyperbolicError": outboundHyperbolicError = val; break;
						case "landedError": landedError = val; break;
						case "highInclinationError": highInclinationError = val; break;
						case "noTransfersError": noTransfersError = val; break;

						case "transferColumnHeader": transferColumnHeader = val; break;
						case "timeColumnHeader": timeColumnHeader = val; break;
						case "deltaVColumnHeader": deltaVColumnHeader = val; break;
						case "columnHeaderTooltip": columnHeaderTooltip = val; break;

						case "maneuverButtonTooltip": maneuverButtonTooltip = val; break;
						case "warpButtonTooltip": warpButtonTooltip = val; break;

						case "yearsValue": yearsValue = val; break;
						case "daysValue": daysValue = val; break;
						case "hoursValue": hoursValue = val; break;
						case "minutesValue": minutesValue = val; break;
						case "secondsValue": secondsValue = val; break;

						case "settingsButtonTooltip": settingsButtonTooltip = val; break;

						case "translationControlsNotification": translationControlsNotification = val; break;

						case "adjustManeuversMessage": adjustManeuversMessage = val; break;

						case "manualLink": manualLink = val; break;
						case "manualLinkTooltip": manualLinkTooltip = val; break;
						case "settingsSectionHeader": settingsSectionHeader = val; break;
						case "planeChangeBurnsSetting": planeChangeBurnsSetting = val; break;
						case "addChangeBurnsSetting": addChangeBurnsSetting = val; break;
						case "autoDeleteNodesSetting": autoDeleteNodesSetting = val; break;
						case "asteroidsSetting": asteroidsSetting = val; break;
						case "maneuverCreationHeader": maneuverCreationHeader = val; break;
						case "autoTargetDestSetting": autoTargetDestSetting = val; break;
						case "autoFocusDestSetting": autoFocusDestSetting = val; break;
						case "autoEditEjecSetting": autoEditEjecSetting = val; break;
						case "autoEditPlaneChgSetting": autoEditPlaneChgSetting = val; break;
						case "autoSetSASSetting": autoSetSASSetting = val; break;
						case "adjustNodesSetting": adjustNodesSetting = val; break;
						case "unitsHeader": unitsHeader = val; break;
						case "metricSetting": metricSetting = val; break;
						case "imperialSetting": imperialSetting = val; break;

					}
				}
			} else {
				DbgFmt("Language file failed to load");
			}
		}

		public static string mainTitle                       { get; private set; }
		public static string mainTooltip                     { get; private set; }

		public static string normalSubtitle                  { get; private set; }
		public static string inboundHyperbolicWarning        { get; private set; }
		public static string outboundHyperbolicError         { get; private set; }
		public static string landedError                     { get; private set; }
		public static string highInclinationError            { get; private set; }
		public static string noTransfersError                { get; private set; }

		public static string transferColumnHeader            { get; private set; }
		public static string timeColumnHeader                { get; private set; }
		public static string deltaVColumnHeader              { get; private set; }
		public static string columnHeaderTooltip             { get; private set; }

		public static string maneuverButtonTooltip           { get; private set; }
		public static string warpButtonTooltip               { get; private set; }

		public static string yearsValue                      { get; private set; }
		public static string daysValue                       { get; private set; }
		public static string hoursValue                      { get; private set; }
		public static string minutesValue                    { get; private set; }
		public static string secondsValue                    { get; private set; }

		public static string settingsButtonTooltip           { get; private set; }

		public static string translationControlsNotification { get; private set; }

		public static string adjustManeuversMessage          { get; private set; }

		public static string manualLink                      { get; private set; }
		public static string manualLinkTooltip               { get; private set; }
		public static string settingsSectionHeader           { get; private set; }
		public static string planeChangeBurnsSetting         { get; private set; }
		public static string addChangeBurnsSetting           { get; private set; }
		public static string autoDeleteNodesSetting          { get; private set; }
		public static string asteroidsSetting                { get; private set; }
		public static string maneuverCreationHeader          { get; private set; }
		public static string autoTargetDestSetting           { get; private set; }
		public static string autoFocusDestSetting            { get; private set; }
		public static string autoEditEjecSetting             { get; private set; }
		public static string autoEditPlaneChgSetting         { get; private set; }
		public static string autoSetSASSetting               { get; private set; }
		public static string adjustNodesSetting              { get; private set; }
		public static string unitsHeader                     { get; private set; }
		public static string metricSetting                   { get; private set; }
		public static string imperialSetting                 { get; private set; }

	}

}
