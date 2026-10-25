#if UNITY_EDITOR || DEVELOPMENT_BUILD || FORGE_QA
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace ForgeEmpire.Tests
{
    public sealed class MobileFrameProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            if(!Application.isMobilePlatform)yield break;
            for(int i=0;i<240;i++)yield return null;
            var samples=new double[300];
            for(int i=0;i<samples.Length;i++) {
                while(!Application.isFocused)yield return null;
                yield return null;samples[i]=Time.unscaledDeltaTime*1000;
            }
            Array.Sort(samples);
            string json="{\"samples\":300,\"medianMs\":"+samples[150].ToString(CultureInfo.InvariantCulture)+",\"p95Ms\":"+samples[285].ToString(CultureInfo.InvariantCulture)+",\"allocatedBytes\":"+Profiler.GetTotalAllocatedMemoryLong()+",\"device\":\""+SystemInfo.deviceModel.Replace("\"","")+"\"}";
            File.WriteAllText(Path.Combine(Application.persistentDataPath,"performance.json"),json);
            Debug.Log("FORGE_MOBILE_PERFORMANCE "+json);
        }
    }
}
#endif
