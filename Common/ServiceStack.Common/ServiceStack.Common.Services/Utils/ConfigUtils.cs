using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;

namespace ServiceStack.Common.Services.Utils
{
    public class ConfigUtils
    {
        const int KEY_INDEX = 0;
        const int VALUE_INDEX = 1;
        const string ERROR_APPSETTING_NOT_FOUND = "Unable to find App Setting: {0}";
        const string ERROR_CONNECTION_STRING_NOT_FOUND = "Unable to find Connection String: {0}";
        const string ERROR_CREATING_TYPE = "Error creating type {0} from text '{1}";
        const char ITEM_SEPERATOR = ',';
        const char KEY_VALUE_SEPERATOR = ':';
        const string CONFIG_NULL_VALUE = "{null}";

        public static string GetNullableAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static string GetAppSetting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (value == null)
            {
                throw new ConfigurationErrorsException(String.Format(ERROR_APPSETTING_NOT_FOUND, key));
            }

            return value;
        }

        /// <summary>
        /// Returns AppSetting[key] if exists otherwise defaultValue
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static string GetAppSetting(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }

        /// <summary>
        /// Returns AppSetting[key] if exists otherwise defaultValue, for non-string values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static T GetAppSetting<T>(string key, T defaultValue)
        {
            string val = ConfigurationManager.AppSettings[key];
            if (val != null)
            {
                if (CONFIG_NULL_VALUE.EndsWith(val))
                {
                    return default(T);
                }
                return ParseTextValue<T>(ConfigurationManager.AppSettings[key]);
            }
            return defaultValue;
        }

        public static ConnectionStringSettings GetConnectionStringSetting(string key)
        {
            var value = ConfigurationManager.ConnectionStrings[key];
            if (value == null)
            {
                throw new ConfigurationErrorsException(String.Format(ERROR_CONNECTION_STRING_NOT_FOUND, key));
            }

            return value;
        }

        public static string GetConnectionString(string key)
        {
            return GetConnectionStringSetting(key).ToString();
        }

        public static List<string> GetListFromAppSetting(string key)
        {
            return new List<string>(GetAppSetting(key).Split(ITEM_SEPERATOR));
        }

        public static Dictionary<string, string> GetDictionaryFromAppSetting(string key)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var item in GetAppSetting(key).Split(ITEM_SEPERATOR))
            {
                var keyValuePair = item.Split(KEY_VALUE_SEPERATOR);
                dictionary.Add(keyValuePair[KEY_INDEX], keyValuePair[VALUE_INDEX]);
            }
            return dictionary;
        }

        #region Private Methods
        /// <summary>
        /// Get the static Parse(string) method on the type supplied
        /// </summary>
        /// <param name="type"></param>
        /// <returns>A delegate to the type's Parse(string) if it has one</returns>
        private static MethodInfo GetParseMethod(Type type)
        {
            const string PARSE_METHOD = "Parse";
            if (type == typeof(string))
            {
                return typeof(ConfigUtils).GetMethod(PARSE_METHOD, BindingFlags.Public | BindingFlags.Static);
            }
            var parseMethodInfo = type.GetMethod(PARSE_METHOD,
                                                    BindingFlags.Public | BindingFlags.Static, null,
                                                    new Type[] { typeof(string) }, null);

            return parseMethodInfo;
        }

        /// <summary>
        /// Gets the constructor info for T(string) if exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private static ConstructorInfo GetConstructorInfo(Type type)
        {
            foreach (ConstructorInfo ci in type.GetConstructors())
            {
                var ciTypes = ci.GetGenericArguments();
                var matchFound = (ciTypes.Length == 1 && ciTypes[0] == typeof(string)); //e.g. T(string)
                if (matchFound)
                {
                    return ci;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the value returned by the 'T.Parse(string)' method if exists otherwise 'new T(string)'. 
        /// e.g. if T was a TimeSpan it will return TimeSpan.Parse(textValue).
        /// If there is no Parse Method it will attempt to create a new instance of the destined type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="textValue">The default value.</param>
        /// <returns>T.Parse(string) or new T(string) value</returns>
        private static T ParseTextValue<T>(string textValue)
        {
            var parseMethod = GetParseMethod(typeof(T));
            if (parseMethod == null)
            {
                var ci = GetConstructorInfo(typeof(T));
                if (ci == null)
                {
                    throw new TypeLoadException(string.Format(ERROR_CREATING_TYPE, typeof(T).Name, textValue));
                }
                var newT = ci.Invoke(null, new object[] { textValue });
                return (T)newT;
            }
            var value = parseMethod.Invoke(null, new object[] { textValue });
            return (T)value;
        }
        #endregion

    }
}