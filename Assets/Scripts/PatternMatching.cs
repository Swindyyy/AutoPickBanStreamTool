using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.UI;
using System;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class PatternMatching : MonoBehaviour
{
    [SerializeField] List<iconToCompare> iconsList = new List<iconToCompare>();

    TeamManager manager;

    Color32[] colors;

    private void Start()
    {
        manager = TeamManager.Instance;
        iconsList = manager.GetIcons();
    }

    public iconToCompare AnalysePattern(IconPosition playerIconToIdentify, Color32[] playerIcon)
        {
        Debug.Log("Analysing pattern");        
        Dictionary<iconToCompare, DMatch[]> championConfidence = new Dictionary<iconToCompare, DMatch[]>();

        foreach (var champion in iconsList)
        {
            //Load texture
            Mat image = OpenCvSharp.Unity.PixelsToMat(playerIcon, playerIconToIdentify.width, playerIconToIdentify.height, true, true, 0);
            Mat dst = new Mat();
            Size scaleSize = new Size(image.Width * 5, image.Height * 5 );
            Cv2.Resize(image, dst, scaleSize);
            Mat imageBW = dst.CvtColor(ColorConversionCodes.BGR2GRAY);

            Mat testMatch = OpenCvSharp.Unity.PixelsToMat(champion.pixels, champion.width, champion.height, false, false, 0);
            Mat testMatchBW = testMatch.CvtColor(ColorConversionCodes.BGR2GRAY);

            var orb = ORB.Create();
            KeyPoint[] kpReferenceImage = new KeyPoint[0];
            Mat desReferenceImage = new Mat();

            KeyPoint[] kpIngameImage = new KeyPoint[0];
            Mat desIngameImage = new Mat();

            orb.DetectAndCompute(testMatchBW, null, out kpReferenceImage, desReferenceImage);
            orb.DetectAndCompute(imageBW, null, out kpIngameImage, desIngameImage);

            var bf = new BFMatcher(NormTypes.Hamming);
            var matches = bf.Match(desIngameImage, desReferenceImage);
            var distances = new List<float>();

            foreach(var match in matches)
            {
                distances.Add(match.Distance);
            }

            Array.Sort(matches, delegate (DMatch match1, DMatch match2)
            {
                return match1.Distance.CompareTo(match2.Distance);
            });

            var topMatches = new DMatch[10];
            Array.ConstrainedCopy(matches, 0, topMatches, 0, topMatches.Length);
            championConfidence.Add(champion, topMatches);

        }

        KeyValuePair<iconToCompare, DMatch[]> highestMatch = new KeyValuePair<iconToCompare, DMatch[]>(new iconToCompare(), null);
        foreach (var keyValuePair in championConfidence)
        {
            if (highestMatch.Value == null)
            {
                highestMatch = keyValuePair;
            } else
            {
               
                if (DistanceAvg(keyValuePair.Value) < DistanceAvg(highestMatch.Value))
                {
                    highestMatch = keyValuePair;
                }
            }
        }


        Debug.Log("Best guess: " + highestMatch.Key.name);
        return highestMatch.Key;
    }

    public iconToCompare AnalysePatternMultiThreaded(IconPosition playerIconToIdentify, Color32[] playerIcon)
    {
        Dictionary<iconToCompare, float[]> championConfidence = new Dictionary<iconToCompare, float[]>();

        NativeArray<CompareChampionParametersContainer> championIcons = new NativeArray<CompareChampionParametersContainer>(iconsList.Count, Allocator.TempJob);
        NativeArray<DistanceArrayContainer> distanceArray = new NativeArray<DistanceArrayContainer>(iconsList.Count, Allocator.TempJob);

        for (int i = 0; i < iconsList.Count; i++)
        {
            championIcons[i] = new CompareChampionParametersContainer
            {
                toCompareChampionIcon = iconsList[i],
                toCompareChampionIconDimensionsX = iconsList[i].width,
                toCompareChampionIconDimensionsY = iconsList[i].height,
                ingameIconPosition = playerIconToIdentify
            };
        }

        CompareChampionJob compareChampionJob = new CompareChampionJob
        {
            ingameChampionIcon = playerIcon,
            parameters = championIcons,
            result = distanceArray
        };

        JobHandle jobHandle = compareChampionJob.Schedule(iconsList.Count, 5);
        jobHandle.Complete();

        foreach(var result in distanceArray)
        {
            championConfidence.Add(result.icon, result.distanceArray);
        }

        championIcons.Dispose();
        distanceArray.Dispose();

        KeyValuePair<iconToCompare, float[]> highestMatch = new KeyValuePair<iconToCompare, float[]>(new iconToCompare(), null);
        foreach (var keyValuePair in championConfidence)
        {
            if (highestMatch.Value == null)
            {
                highestMatch = keyValuePair;
            }
            else
            {
                if (DistanceAvg(keyValuePair.Value) < DistanceAvg(highestMatch.Value))
                {
                    highestMatch = keyValuePair;
                }
            }
        }


        Debug.Log("Best guess: " + highestMatch.Key.name);
        return highestMatch.Key;
    }


    float DistanceAvg(float[] matches)
    {
        float dist = 0f;
        foreach(var match in matches)
        {
            dist += match;
        }
        return dist / matches.Length;
    }

    float DistanceAvg(DMatch[] matches)
    {
        float dist = 0f;
        foreach (var match in matches)
        {
            dist += match.Distance;
        }
        return dist / matches.Length;
    }
}


public struct CompareChampionJob : IJobParallelFor
{
    public Color32[] ingameChampionIcon;
    public NativeArray<CompareChampionParametersContainer> parameters;
    public NativeArray<DistanceArrayContainer> result;

    public void Execute(int index)
    {
        //Load texture
        Mat ingameIconMat = OpenCvSharp.Unity.PixelsToMat(ingameChampionIcon, parameters[index].ingameIconPosition.width, parameters[index].ingameIconPosition.height, true, true, 0);
        Mat ingameIconScaled = new Mat();
        Size scaleSize = new Size(ingameIconMat.Width * 5, ingameIconMat.Height * 5);
        Cv2.Resize(ingameIconMat, ingameIconScaled, scaleSize);
        Mat imageBW = ingameIconScaled.CvtColor(ColorConversionCodes.BGR2GRAY);
        Color32[] toCompareIconColor32 = parameters[index].toCompareChampionIcon.pixels;
        Mat toCompareChampionIconMat = OpenCvSharp.Unity.PixelsToMat(toCompareIconColor32, parameters[index].toCompareChampionIconDimensionsX, parameters[index].toCompareChampionIconDimensionsY, true, true, 0);
        Mat toCompareChampionIconBW = toCompareChampionIconMat.CvtColor(ColorConversionCodes.BGR2GRAY);

        var orb = ORB.Create();
        KeyPoint[] kpReferenceImage = new KeyPoint[0];
        Mat desReferenceImage = new Mat();

        KeyPoint[] kpIngameImage = new KeyPoint[0];
        Mat desIngameImage = new Mat();

        orb.DetectAndCompute(toCompareChampionIconBW, null, out kpReferenceImage, desReferenceImage);
        orb.DetectAndCompute(imageBW, null, out kpIngameImage, desIngameImage);

        var bf = new BFMatcher(NormTypes.Hamming);
        var matches = bf.Match(desIngameImage, desReferenceImage);
        var distances = new List<float>();

        foreach (var match in matches)
        {
            distances.Add(match.Distance);
        }

        distances.Sort();
        var distanceArray = distances.ToArray();
        var topMatches = new float[10];
        Array.ConstrainedCopy(distanceArray, 0, topMatches, 0, topMatches.Length);
        result[index] = new DistanceArrayContainer
        {
            icon = parameters[index].toCompareChampionIcon,
            distanceArray = topMatches
        };
    }
}

public struct DistanceArrayContainer
{
    public iconToCompare icon;
    public float[] distanceArray;
}

public struct CompareChampionParametersContainer
{
    public iconToCompare toCompareChampionIcon;
    public int toCompareChampionIconDimensionsX;

    public int toCompareChampionIconDimensionsY;
    public IconPosition ingameIconPosition;
}

