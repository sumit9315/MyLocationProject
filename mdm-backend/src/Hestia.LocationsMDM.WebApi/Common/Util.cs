/*
 * Copyright (c) 2020, TopCoder, Inc. All rights reserved.
 */
using Hestia.LocationsMDM.WebApi.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Globalization;
using Newtonsoft.Json.Converters;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Models.CalendarEvent;
using Hestia.LocationsMDM.WebApi.Common.Constants;
//using ApiIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
//using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Common
{
    /// <summary>
    /// This class contains validation methods.
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Represents the JSON serializer settings.
        /// </summary>
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            }
        };

        /// <summary>
        /// Validates that <paramref name="param"/> is not <c>null</c>.
        /// </summary>
        ///
        /// <typeparam name="T">The type of the parameter, must be reference type.</typeparam>
        ///
        /// <param name="param">The parameter to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        ///
        /// <exception cref="ArgumentNullException">If <paramref name="param"/> is <c>null</c>.</exception>
        public static void ValidateArgumentNotNull<T>(T param, string paramName)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName, $"{paramName} cannot be null.");
            }
        }

        /// <summary>
        /// Validates that <paramref name="param"/> is not <c>null</c> or empty.
        /// </summary>
        ///
        /// <param name="param">The parameter to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        ///
        /// <exception cref="ArgumentNullException">If <paramref name="param"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="param"/> is empty.</exception>
        public static void ValidateArgumentNotNullOrEmpty(string param, string paramName)
        {
            ValidateArgumentNotNull(param, paramName);
            if (string.IsNullOrWhiteSpace(param))
            {
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
            }
        }

        /// <summary>
        /// Validates that <paramref name="param"/> is not <c>null</c> or empty.
        /// </summary>
        ///
        /// <typeparam name="T">Type of items in collection.</typeparam>
        /// <param name="param">The parameter to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        public static void ValidateArgumentNotNullOrEmpty<T>(ICollection<T> param, string paramName)
        {
            ValidateArgumentNotNull(param, paramName);
            if (param.Count == 0)
            {
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
            }
        }

        /// <summary>
        /// Validates that <paramref name="param"/> is not <c>null</c> or an invalid date.
        /// </summary>
        ///
        /// <param name="param">The parameter to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        ///
        /// <exception cref="ArgumentNullException">If <paramref name="param"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="param"/> is an invalid date.</exception>
        public static DateTime ValidateArgumentValidDate(string param, string paramName)
        {
            ValidateArgumentNotNull(param, paramName);
            string[] formats = { "MM/dd/yyyy" };
            DateTime dummyOutput;
            if (!DateTime.TryParseExact(param, formats, null, DateTimeStyles.None, out dummyOutput))
            {
                throw new ArgumentException($"{paramName} is not a valid time 'MM/dd/yyyy'.", paramName);
            }
            return dummyOutput;
        }

        /// <summary>
        /// Validates that <paramref name="param1"/> or <paramref name="param2"/> is not <c>null</c> or an invalid time.
        /// </summary>
        ///
        /// <param name="param1">The parameter1 to validate.</param>
        /// <param name="paramName1">The name1 of the parameter.</param>
        /// <param name="param2">The parameter1 to validate.</param>
        /// <param name="paramName2">The name1 of the parameter.</param>
        /// <param name="format">The format of the input.</param>
        ///
        /// <exception cref="ArgumentNullException">If <paramref name="param1"/> or <paramref name="param2"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="param1"/> or <paramref name="param2"/> is an invalid time or <paramref name="param1"/> is greater or equal to <paramref name="param2"/>.</exception>
        public static void ValidateArgumentValidDateGreater(string param1, string param2, string paramName1, string paramName2, string format = @"hh\:mm\:ss")
        {
            ValidateArgumentNotNull(param1, paramName1);
            DateTime dummyOutput1 = ValidateArgumentValidDate(param1, paramName1);
            DateTime dummyOutput2 = ValidateArgumentValidDate(param2, paramName2);
            if (dummyOutput1 > dummyOutput2)
            {
                throw new ArgumentException($"{paramName2} must be greater or equal than {paramName1}");
            }
        }

        /// <summary>
        /// Validates that <paramref name="param"/> is not <c>null</c> or an invalid time.
        /// </summary>
        ///
        /// <param name="param">The parameter to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="format">The format of the input.</param>
        ///
        /// <exception cref="ArgumentNullException">If <paramref name="param"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="param"/> is an invalid time.</exception>
        public static TimeSpan ValidateArgumentValidTime(string param, string paramName, string format = @"hh\:mm\:ss")
        {
            ValidateArgumentNotNull(param, paramName);
            TimeSpan dummyOutput;
            if (!TimeSpan.TryParseExact(param, format, null, TimeSpanStyles.None, out dummyOutput))
            {
                throw new ArgumentException($"{paramName} is not a valid time '{format}' => {param}.", paramName);
            }
            return dummyOutput;
        }

        /// <summary>
        /// Validates that <paramref name="param1"/> or <paramref name="param2"/> is not <c>null</c> or an invalid time.
        /// </summary>
        ///
        /// <param name="param1">The parameter1 to validate.</param>
        /// <param name="paramName1">The name1 of the parameter.</param>
        /// <param name="param2">The parameter1 to validate.</param>
        /// <param name="paramName2">The name1 of the parameter.</param>
        /// <param name="format">The format of the input.</param>
        ///
        /// <exception cref="ArgumentNullException">If <paramref name="param1"/> or <paramref name="param2"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="param1"/> or <paramref name="param2"/> is an invalid time or <paramref name="param1"/> is greater than <paramref name="param2"/>.</exception>
        public static void ValidateArgumentValidTimeGreater(string param1, string param2, string paramName1, string paramName2, string format = @"hh\:mm\:ss")
        {
            ValidateArgumentNotNull(param1, paramName1);
            TimeSpan dummyOutput1 = ValidateArgumentValidTime(param1, paramName1, format);
            TimeSpan dummyOutput2 = ValidateArgumentValidTime(param2, paramName2, format);
            if (param2 != "00:00" && param2 != "00:00:00" && dummyOutput1 > dummyOutput2)
            {
                throw new ArgumentException($"{paramName2} must be greater or equal than {paramName1}");
            }
        }

        /// <summary>
        /// Validates that KOB is valid for the given loca
        /// </summary>
        /// <param name="locationType"></param>
        /// <param name="kob"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void ValidateLocationTypeKOB(string locationType, string kob)
        {
            if (!KOBs.IsValidLocationTypeKOB(locationType, kob))
            {
                throw new ArgumentException($"'{kob}' KOB is not valid for '{locationType}' Location Type.");
            }
        }

        /// <summary>
        /// Gets the partition key based on the node type.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="config">The Cosmos DB configuration.</param>
        /// <returns>The partition key for the given node type.</returns>
        public static string GetPartitionKey(this NodeType nodeType, CosmosConfig config)
        {
            return nodeType switch
            {
                NodeType.Campus => config.CampusPartitionKey,
                NodeType.Region => config.RegionPartitionKey,
                NodeType.ChildLoc => config.ChildLocationPartitionKey,
                _ => throw new InvalidOperationException($"Unknown node type: {nodeType}"),
            };
        }

        /// <summary>
        /// Gets the Node Type based on the partition key.
        /// </summary>
        /// <param name="config">The Cosmos DB configuration.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <returns>Type of the node.</returns>
        public static NodeType GetNodeType(CosmosConfig config, string partitionKey)
        {
            if (partitionKey == config.CampusPartitionKey)
            {
                return NodeType.Campus;
            }
            if (partitionKey == config.RegionPartitionKey)
            {
                return NodeType.Region;
            }
            if (partitionKey == config.ChildLocationPartitionKey)
            {
                return NodeType.ChildLoc;
            }

            throw new InvalidOperationException($"Unknown Location partition key: {partitionKey}");
        }

        /// <summary>
        /// Performs actions for each operation.
        /// </summary>
        /// <typeparam name="T">Type of the items.</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="action">The action.</param>
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }

        /// <summary>
        /// Adds given collection of items to the existing list.
        /// </summary>
        /// <typeparam name="T">Type of the items.</typeparam>
        /// <param name="items">The existing list of items.</param>
        /// <param name="itemsToAdd">The collection of items to add.</param>
        public static void AddRange<T>(this IList<T> items, IEnumerable<T> itemsToAdd)
        {
            foreach (var item in itemsToAdd)
            {
                items.Add(item);
            }
        }

        /// <summary>
        /// Joins strings using provided separator.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="separator">The separator to use.</param>
        public static string StringJoin(this IEnumerable<string> items, string separator)
        {
            var result = string.Join(separator, items);
            return result;
        }

        /// <summary>
        /// Gets the end of the day date-time of the specified date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The end of the day date-time.</returns>
        public static DateTime? EOD(this DateTime? date)
        {
            if (date == null)
            {
                return null;
            }

            return date.Value.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Checks the range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="fieldName">Name of the field.</param>
        internal static void CheckRange(int? value, int minValue, int maxValue, string fieldName)
        {
            if (value == null)
            {
                return;
            }

            if (value < minValue || value > maxValue)
            {
                throw new ArgumentException($"{fieldName} should be within [{minValue}..{maxValue}] range.");
            }
        }

        /// <summary>
        /// Gets the string list.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The string list.</returns>
        public static List<string> GetStringList(this JToken source, string propertyName)
        {
            var property = source[propertyName];
            if (property != null)
            {
                if (property is JArray jArr)
                {
                    return jArr.ToObject<List<string>>();
                }
                else if (property is JValue jVal)
                {
                    var result = new List<string>();
                    string stringVal = jVal.ToString();
                    if (!string.IsNullOrWhiteSpace(stringVal))
                    {
                        // single value (if not null/empty) should be placed in to the list
                        result.Add(stringVal);
                    }
                    return result;
                }
            }

            return new List<string>();
        }

        public static string GetCalendarEventMassUpdateFilterDescription(CalendarEventMassUpdateFilter filter)
        {
            if (filter == null)
            {
                return null;
            }

            var sb = new StringBuilder();

            AppendValueIfNotEmpty(sb, "City", filter.CityName);
            AppendValueIfNotEmpty(sb, "State", filter.State);
            AppendValueIfNotEmpty(sb, "Country", filter.CountryName);
            AppendValueIfNotEmpty(sb, "KOB", filter.Kob);
            AppendValueIfNotEmpty(sb, "Location Type", filter.LocationType);
            AppendValueIfNotEmpty(sb, "Location IDs", filter.ChildLocNodes, putValueOnNewLine: true);
            AppendValueIfNotEmpty(sb, "Cost Center IDs", filter.ChildLocNodes, putValueOnNewLine: true);
            AppendValueIfNotEmpty(sb, "Excluded Location IDs", filter.ExcludedLocationNodes?.StringJoin(Environment.NewLine), putValueOnNewLine: true);

            return sb.ToString();
        }

        private static void AppendValueIfNotEmpty(StringBuilder sb, string key, string value, bool putValueOnNewLine = false)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                sb.Append($"{key}: ");
                if (putValueOnNewLine)
                {
                    sb.AppendLine();
                }

                sb.AppendLine(value);
            }
        }

        /// <summary>
        /// Gets the object list.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The object list.</returns>
        public static List<JObject> GetObjectList(this JToken source, string propertyName)
        {
            var property = source[propertyName];
            if (property != null && property is JArray jArr)
            {
                return jArr.OfType<JObject>().ToList();
            }

            return new List<JObject>();
        }

        public static List<HierarchyNode> OrderStructureByName(IList<HierarchyNode> items)
        {
            var result = items.OrderBy(x => x.Name.ToLower()).ToList();
            foreach (var item in items)
            {
                if (item.Children?.Count > 1)
                {
                    item.Children = OrderStructureByName(item.Children);
                }
            }

            return result;
        }

        public static bool IsSequenceSame(this JToken token1, JToken token2)
        {
            if (token1 == null && token2 == null)
            {
                return true;
            }

            if (token1 == null || token2 == null)
            {
                return false;
            }

            return token1.SequenceEqual(token2);
        }

        public static bool HasChanges(JObject obj1, JObject obj2, params string[] propsToCheck)
        {
            foreach (var prop in propsToCheck)
            {
                if (obj1[prop].ToString() != obj2[prop].ToString())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the associates.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public static IList<AssociateModel> GetAssociates(this JToken source, string propertyName = "associate")
        {
            var property = source[propertyName];
            return property == null
                ? new List<AssociateModel>()
                : property.ToObject<IList<AssociateModel>>();
        }

        /// <summary>
        /// Converts Associate model to Titled Contacts.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static IList<TitledContact> ToTitledContacts(this IList<AssociateModel> source)
        {
            if (source == null)
            {
                return new List<TitledContact>();
            }

            var result = new List<TitledContact>();
            foreach (var associate in source)
            {
                var values = new string[] { associate.FirstName, associate.MiddleName, associate.LastName };
                var nonEmptyValues = values.Where(x => !string.IsNullOrWhiteSpace(x));
                var name = string.Join(" ", nonEmptyValues);
                result.Add(new TitledContact
                {
                    AssociateId = associate.AssociateId,
                    Name = name,
                    Title = associate.Title
                });
            }

            return result;
        }

        /// <summary>
        /// Updates the property with given value, in case value is not null.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="jToken">The source object.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="value">The value to set.</param>
        public static void UpdateProperty<T>(this JToken jToken, string propName, T value)
        {
            jToken[propName] = value != null
                ? JToken.FromObject(value, JsonSerializer.Create(SerializerSettings))
                : null;
        }

        /// <summary>
        /// Updates the optional property with given value, in case value is not null.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="jToken">The source object.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="value">The value to set (in case not null).</param>
        public static void UpdateOptionalProperty<T>(this JToken jToken, string propName, T value)
        {
            if (value != null)
            {
                jToken[propName] = JToken.FromObject(value, JsonSerializer.Create(SerializerSettings));
            }
        }

        /// <summary>
        /// Updates the optional property with given value, in case condition is true.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="jToken">The source object.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="value">The value to set (in case not null).</param>
        public static void UpdateOptionalProperty<T>(this JToken jToken, string propName, bool condition, T value)
        {
            if (condition)
            {
                jToken[propName] = JToken.FromObject(value, JsonSerializer.Create(SerializerSettings));
            }
        }

        /// <summary>
        /// Converts boolean to 'Y' or 'N' string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="emptyIfNull">Will return empty string in case value is null, otherwise will return null.</param>
        /// <returns>
        /// 'Y' or 'N' string.
        /// </returns>
        public static string ToShortYesNo(this bool? value, bool emptyIfNull = true)
        {
            if (value == null)
            {
                return emptyIfNull ? string.Empty : null;
            }

            return value == true ? "Y" : "N";
        }

        /// <summary>
        /// Converts boolean to 'Y' or 'N' string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 'Y' or 'N' string.
        /// </returns>
        public static string ToShortYesNo(this bool value)
        {
            return value == true ? "Y" : "N";
        }

        /// <summary>
        /// Converts 'Y' or 'N' string to boolean.
        /// </summary>
        /// <returns>true for 'Y', false otherwise.</returns>
        public static bool FromShortYesNo(this JToken token)
        {
            if (token != null)
            {
                return token.Value<string>()?.ToLower() == "y";
            }

            return false;
        }

        /// <summary>
        /// Appends the contains condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="criteriaValue">The criteria value.</param>
        /// <returns>The updated query string builder.</returns>
        public static StringBuilder AppendContainsCondition(this StringBuilder sb, string propName, string criteriaValue)
        {
            if (!string.IsNullOrWhiteSpace(criteriaValue))
            {
                // escape ' character
                criteriaValue = criteriaValue.Replace("'", "\\'");
                sb = sb.Append($" and CONTAINS(c.{propName}, '{criteriaValue.Trim()}', true)");
            }
            return sb;
        }

        /// <summary>
        /// Appends the contains in Array condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="criteriaValue">The criteria value.</param>
        /// <returns>The updated query string builder.</returns>
        public static StringBuilder AppendArrayContainsCondition(this StringBuilder sb, string propName, string criteriaValue)
        {
            if (!string.IsNullOrWhiteSpace(criteriaValue))
            {
                sb = sb.Append($" and ARRAY_CONTAINS(c.{propName}, '{criteriaValue.Trim()}', true)");
            }
            return sb;
        }

        /// <summary>
        /// Appends the contains OR condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName1">Name of the property 1.</param>
        /// <param name="propName2">Name of the property 2.</param>
        /// <param name="criteriaValue">The criteria value.</param>
        /// <returns>The updated query string builder.</returns>
        public static StringBuilder AppendContainsOrCondition(this StringBuilder sb, string propName1, string propName2, string criteriaValue)
        {
            if (!string.IsNullOrWhiteSpace(criteriaValue))
            {
                sb = sb.Append($" and (CONTAINS(c.{propName1}, '{criteriaValue}', true) or CONTAINS(c.{propName2}, '{criteriaValue.Trim()}', true))");
            }
            return sb;
        }

        /// <summary>
        /// Appends the contains OR condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName">List of properties.</param>
        /// <param name="criteriaValue">The criteria value.</param>
        /// <returns>The updated query string builder.</returns>
        public static StringBuilder AppendContainsOrCondition(this StringBuilder sb, IList<string> propName, string criteriaValue)
        {
            if (!string.IsNullOrWhiteSpace(criteriaValue))
            {
                IEnumerable<string> mappedProp = propName.Select(str => $"CONTAINS(c.{str}, '{criteriaValue.Trim()}', true)");
                string joined = string.Join(" or ", mappedProp.ToArray());
                sb = sb.Append($" and ({joined})");
            }
            return sb;
        }

        /// <summary>
        /// Appends the contains condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="values">The values.</param>
        /// <returns>The updated query string builder.</returns>
        public static StringBuilder AppendContainsCondition(this StringBuilder sb, string propName, IList<string> values)
        {
            var normalizedValues = GetNormalizedValues(values);
            if (normalizedValues.Any())
            {
                var valuesCsv = string.Join("','", normalizedValues);
                sb = sb.Append($" AND c.{propName} IN('{valuesCsv}')");
            }

            return sb;
        }

        /// <summary>
        /// Appends the contains NOT in condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="values">The values.</param>
        /// <returns>The updated query string builder.</returns>
        public static StringBuilder AppendNotContainsCondition(this StringBuilder sb, string propName, IList<string> values)
        {
            var normalizedValues = GetNormalizedValues(values);
            if (normalizedValues.Any())
            {
                var valuesCsv = string.Join("','", normalizedValues);
                sb = sb.Append($" AND c.{propName} NOT IN('{valuesCsv}')");
            }
            return sb;
        }

        /// <summary>
        /// Gets the normalized values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        private static IEnumerable<string> GetNormalizedValues(IList<string> values)
        {
            if (values == null)
            {
                return Enumerable.Empty<string>();
            }

            return values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim());
        }

        /// <summary>
        /// Appends the Equals condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="criteriaValue">The criteria value.</param>
        /// <returns>The updated query string builder.</returns>
        public static StringBuilder AppendEqualsCondition(this StringBuilder sb, string propName, string criteriaValue)
        {
            if (!string.IsNullOrWhiteSpace(criteriaValue))
            {
                sb = sb.Append($" and STRINGEQUALS(c.{propName}, '{criteriaValue.Trim()}', true)");
            }
            return sb;
        }

        /// <summary>
        /// Appends the '>=' date-time condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="date">The date-time criteria value.</param>
        /// <returns>
        /// The updated query string builder.
        /// </returns>
        public static StringBuilder AppendGreaterThanOrEqualToCondition(this StringBuilder sb, string propName, DateTime? date)
        {
            if (date != null)
            {
                sb = sb.Append($" AND c.{propName} >= '{date.Value:O}'");
            }
            return sb;
        }

        /// <summary>
        /// Appends the 'Less Than or Equal To' date-time condition.
        /// </summary>
        /// <param name="sb">The query string builder.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="date">The date-time criteria value.</param>
        /// <returns>
        /// The updated query string builder.
        /// </returns>
        public static StringBuilder AppendLessThanOrEqualToCondition(this StringBuilder sb, string propName, DateTime? date)
        {
            if (date != null)
            {
                sb = sb.Append($" AND c.{propName} <= '{date.Value:O}'");
            }
            return sb;
        }

        public static string[] SplitMultivalueCriteria(string criteria)
        {
            if (string.IsNullOrWhiteSpace(criteria))
            {
                return new string[0];
            }

            var result = criteria.Split(
                new char[] { ' ', ',', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            return result;
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Serialized data,</returns>
        public static string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, SerializerSettings);
        }

        public static JToken ToJToken<T>(T value)
        {
            var serializer = JsonSerializer.Create(SerializerSettings);
            return JToken.FromObject(value, serializer);
        }

        public static JObject ToJObject<T>(T value)
        {
            var serializer = JsonSerializer.Create(SerializerSettings);
            return JObject.FromObject(value, serializer);
        }

        /// <summary>
        /// Converts given date/time to string in UTC format.
        /// </summary>
        /// <param name="value">The date/time value.</param>
        /// <returns>The time in UTC format.</returns>
        public static string ToUtcTimeString(this DateTime value)
        {
            // Example format: '2021-12-30T12:00:00Z'
            string result = value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            return result;
        }

        /// <summary>
        /// Converts given date/time to string in UTC format.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="hour">The hour.</param>
        /// <param name="minute">The minute.</param>
        /// <param name="second">The second.</param>
        /// <returns>The time in UTC format.</returns>
        public static string ToUtcTimeString(int year, int month = 1, int day = 1, int hour = 0, int minute = 0, int second = 0)
        {
            // Example format: '2021-12-30T12:00:00Z'
            var datetime = new DateTime(year, month, day, hour, minute, second);
            var result = datetime.ToUtcTimeString();
            return result;
        }


        /// <summary>
        /// Cache of current logged in user.
        /// </summary>
        private static LoggedInUser currentUser;

        /// <summary>
        /// Converts to loggedinuser.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns>The logged in user details.</returns>
        public static LoggedInUser ToLoggedInUser(this ClaimsPrincipal claimsPrincipal)
        {
            var username = claimsPrincipal.GetClaim("name", isRequired: false);
            var givename = claimsPrincipal.GetClaim(ClaimTypes.GivenName, isRequired: false);
            var surname = claimsPrincipal.GetClaim(ClaimTypes.Surname, isRequired: false);
            var email = claimsPrincipal.GetClaim(ClaimTypes.Email, isRequired: false);
            var groups = claimsPrincipal.GetClaims("groups", isRequired: false);
            var id = claimsPrincipal.GetClaim("http://schemas.microsoft.com/identity/claims/objectidentifier", isRequired: false);

            currentUser = new LoggedInUser
                {
                    Id = id,
                    GivenName = givename,
                    FamilyName = surname,
                    Email = email,
                    Username = username,
                    Role = currentUser?.Role ?? "user",
                    Roles = GetRoles(groups)
                };
            return currentUser;
        }

        /// <summary>
        /// Gets the claim value.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <param name="claimType">Type of the claim.</param>
        /// <param name="isRequired">if set to <c>true</c> [is required].</param>
        /// <returns>The claim value.</returns>
        public static string GetClaim(this ClaimsPrincipal claimsPrincipal, string claimType, bool isRequired = false)
        {
            Claim claim = FindClaim(claimsPrincipal, claimType, isRequired);

            return claim?.Value;
        }

        /// <summary>
        /// Gets the claim value.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <param name="claimType">Type of the claim.</param>
        /// <param name="isRequired">if set to <c>true</c> [is required].</param>
        /// <returns>The claim value.</returns>
        public static IEnumerable<Claim> GetClaims(this ClaimsPrincipal claimsPrincipal, string claimType, bool isRequired = false)
        {
            var claim = claimsPrincipal.Claims.Where(x => x.Type == claimType);
            if (claim.Count() == 0 && isRequired)
            {
                throw new ServiceException($"'{claimType}' claim type is missing.");
            }
            return claim;
        }

        /// <summary>
        /// Finds the claim.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <param name="claimType">Type of the claim.</param>
        /// <param name="isRequired">if set to <c>true</c> [is required].</param>
        /// <returns>Found claim.</returns>
        private static Claim FindClaim(ClaimsPrincipal claimsPrincipal, string claimType, bool isRequired)
        {
            var claim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == claimType);
            if (claim == null && isRequired)
            {
                throw new ServiceException($"'{claimType}' claim type is missing.");
            }

            return claim;
        }

        /// <summary>
        /// Gets groups as string.
        /// </summary>
        ///<param name="groups">The list of Azure AD groups.</param>
        /// <returns>List of groups</returns>
        private static IList<string> GetRoles(IEnumerable<Claim> groups)
        {
            return groups.Select(x => x?.Value).ToList();
        }
    }
}
