using System;
using System.Collections;
using System.Globalization;
using System.Linq;
#if NET_4_0_ABOVE
using System.Reflection;
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Base class for exceptions that have actual and expected values
	/// </summary>
	public class AssertActualExpectedException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see href="AssertActualExpectedException"/> class.
		/// </summary>
		/// <param name="expected">The expected value</param>
		/// <param name="actual">The actual value</param>
		/// <param name="userMessage">The user message to be shown</param>
		/// <param name="expectedTitle">The title to use for the expected value (defaults to "Expected")</param>
		/// <param name="actualTitle">The title to use for the actual value (defaults to "Actual")</param>
		public AssertActualExpectedException(object expected, object actual, string userMessage, string expectedTitle = null, string actualTitle = null)
				: base(userMessage)
		{
			Actual = actual == null ? null : ConvertToString(actual);
			ActualTitle = actualTitle ?? "Actual";
			Expected = expected == null ? null : ConvertToString(expected);
			ExpectedTitle = expectedTitle ?? "Expected";

			if (actual != null &&
					expected != null &&
					Actual == Expected &&
					actual.GetType() != expected.GetType())
			{
				Actual += string.Format(CultureInfo.CurrentCulture, " ({0})", actual.GetType().FullName);
				Expected += string.Format(CultureInfo.CurrentCulture, " ({0})", expected.GetType().FullName);
			}
		}

		/// <summary>
		/// Gets the actual value.
		/// </summary>
		public string Actual { get; private set; }

		/// <summary>
		/// Gets the title used for the actual value.
		/// </summary>
		public string ActualTitle { get; private set; }

		/// <summary>
		/// Gets the expected value.
		/// </summary>
		public string Expected { get; private set; }

		/// <summary>
		/// Gets the title used for the expected value.
		/// </summary>
		public string ExpectedTitle { get; private set; }

		/// <summary>
		/// Gets a message that describes the current exception. Includes the expected and actual values.
		/// </summary>
		/// <returns>The error message that explains the reason for the exception, or an empty string("").</returns>
		/// <filterpriority>1</filterpriority>
		public override string Message
		{
			get
			{
				var titleLength = Math.Max(ExpectedTitle.Length, ActualTitle.Length) + 2;  // + the colon and space
				var formattedExpectedTitle = (ExpectedTitle + ":").PadRight(titleLength);
				var formattedActualTitle = (ActualTitle + ":").PadRight(titleLength);

				return string.Format(CultureInfo.CurrentCulture,
														 "{0}{5}{1}{2}{5}{3}{4}",
														 base.Message,
														 formattedExpectedTitle,
														 Expected ?? "(null)",
														 formattedActualTitle,
														 Actual ?? "(null)",
														 Environment.NewLine);
			}
		}

#if NET_4_0_ABOVE
		private static string ConvertToSimpleTypeName(TypeInfo typeInfo)
#else
		static string ConvertToSimpleTypeName(Type typeInfo)
#endif
		{
			if (!typeInfo.IsGenericType) { return typeInfo.Name; }

#if NET_4_0_ABOVE
			var simpleNames = typeInfo.GenericTypeArguments.Select(type => ConvertToSimpleTypeName(type.GetTypeInfo()));
#else
			var simpleNames = typeInfo.GetGenericArguments().Select(type => ConvertToSimpleTypeName(type));
#endif
			var backTickIdx = typeInfo.Name.IndexOf('`');
			if (backTickIdx < 0)
				backTickIdx = typeInfo.Name.Length;  // F# doesn't use backticks for generic type names

			return String.Format("{0}<{1}>", typeInfo.Name.Substring(0, backTickIdx), String.Join(", ", simpleNames));
		}

		private static string ConvertToString(object value)
		{
			var stringValue = value as string;
			if (stringValue != null)
				return stringValue;

			var formattedValue = ArgumentFormatter.Format(value);
			if (value is IEnumerable)
			{
#if NET_4_0_ABOVE
				formattedValue = String.Format("{0} {1}", ConvertToSimpleTypeName(value.GetType().GetTypeInfo()), formattedValue);
#else
				formattedValue = String.Format("{0} {1}", ConvertToSimpleTypeName(value.GetType()), formattedValue);
#endif
			}

			return formattedValue;
		}
	}
}