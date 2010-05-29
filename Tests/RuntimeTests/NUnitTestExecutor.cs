﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace TechTalk.SpecFlow.RuntimeTests
{
    class NUnitTestExecutor
    {
        public static void ExecuteNUnitTests(object test, Func<Exception, bool> onError)
        {
            // fixture setup
            ExecuteWithAttribute(test, typeof(TestFixtureSetUpAttribute));

            foreach (var testMethod in GetMethodsWithAttribute(test, typeof(TestAttribute)))
            {
                try
                {
                    Debug.WriteLine(testMethod, "Executing test");

                    // test setup
                    ExecuteWithAttribute(test, typeof(SetUpAttribute));

                    InvokeMethod(test, testMethod);

                    // test teardown
                    ExecuteWithAttribute(test, typeof(TearDownAttribute));
                }
                catch(Exception ex)
                {
                    if (onError == null || !onError(ex))
                        throw;
                }
            }

            // fixture teardown
            ExecuteWithAttribute(test, typeof(TestFixtureTearDownAttribute));
        }

        private static void InvokeMethod(object test, MethodInfo testMethod)
        {
            try
            {
                testMethod.Invoke(test, null);
            }
            catch (TargetInvocationException invEx)
            {
                var ex = invEx.InnerException;
                PreserveStackTrace(ex);
                throw ex;
            }
        }

        internal static void PreserveStackTrace(Exception ex)
        {
			Type exceptionType = typeof(Exception);
			
			// Mono's implementation of System.Exception doesn't contain the method InternalPreserveStackTrace
			if (Type.GetType("Mono.Runtime") != null)
			{
				FieldInfo remoteStackTraceString = exceptionType.GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
				
				// Just in case we're running in pre-2.6
				if (remoteStackTraceString == null)
				{
					remoteStackTraceString = exceptionType.GetField("remote_stack_trace", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				
				remoteStackTraceString.SetValue(ex, ex.StackTrace + Environment.NewLine);
			}
			else
			{
				exceptionType.GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ex, new object[0]);
			}
        }

        private static void ExecuteWithAttribute(object test, Type attributeType)
        {
            foreach (var methodInfo in GetMethodsWithAttribute(test, attributeType))
            {
                InvokeMethod(test, methodInfo);
            }
        }

        private static IEnumerable<MethodInfo> GetMethodsWithAttribute(object test, Type attributeType)
        {
            foreach (var methodInfo in test.GetType().GetMethods())
            {
                var attr = Attribute.GetCustomAttribute(methodInfo, attributeType, true);
                if (attr == null)
                    continue;

                yield return methodInfo;
            }
        }
    }
}
