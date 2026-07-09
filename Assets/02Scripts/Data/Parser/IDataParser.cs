using System.Collections.Generic;
using UnityEngine;

public interface IDataParser
{
    T Parse<T>(string json);
}