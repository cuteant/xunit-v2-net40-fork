﻿using System;
using System.Linq.Expressions;
using Xunit;

#if NET40
namespace ExcelDataExamples40
#else
namespace ExcelDataExamples45
#endif
{
	public class ExcelDataExamples
	{
		[Theory]
		[ExcelData("UnitTestData.xls", "select * from [Sheet1$A1:C5]")]
		public void ExcelXlsTests(int x, string y, string z)
		{
			Assert.NotEqual("Baz", z);
		}
	}
}