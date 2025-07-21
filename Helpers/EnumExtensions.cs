using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

// Projenizin ana namespace'i ile aynı yapın. Genellikle proje adıdır.
namespace BelbimEnv
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>()?
                .GetName() ?? enumValue.ToString();
        }
    }
}