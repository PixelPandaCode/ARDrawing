using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Vectorization : MonoBehaviour
{
    public float minStep = 0.05f;
    public float minAngle = 5f;
     
    public void Combine_Step(ref List<CursorData> data)
    {
        // Step 1: Regenerate by accumulating distance
        List<CursorData> consolidatedData = new List<CursorData>
        {
            data[0]
        };
        Vector3 lastPosition = data[0].pointerPos;
        float accumulateDist = 0.0f;
        for (int i = 1; i < data.Count; i++)
        {
            Vector3 currentPosition = data[i].pointerPos;
            accumulateDist += Vector3.Distance(lastPosition, currentPosition);
            if (accumulateDist >= minStep)
            {
                consolidatedData.Add(data[i]);
                accumulateDist = 0.0f;
            }
            lastPosition = currentPosition;
        }
        if (accumulateDist != 0.0f)
        {
            consolidatedData.Add(data[data.Count - 1]);
        }

        data = consolidatedData;
    }

    public void Combine_Angle(ref List<CursorData> data)
    {
        // Step 2: Combine vectors with small angle differences
        if (data.Count <= 2)
        {
            return;
        }
        List<CursorData> combinedData = new List<CursorData>
        {
            data[0],
            data[1]
        };

        for (int i = 2; i < data.Count; i++)
        {
            Vector3 curDir = (combinedData[combinedData.Count - 1].pointerPos - combinedData[combinedData.Count - 2].pointerPos).normalized;
            Vector3 newDir = (data[i].pointerPos - combinedData[combinedData.Count - 1].pointerPos).normalized;
            float angle = Vector3.Angle(curDir, newDir);
            if (angle <= minAngle)
            {
                combinedData[combinedData.Count - 1] = data[i];
            }
            else
            {
                combinedData.Add(data[i]);
            }
        }

        data = combinedData;
    }

    public void Smooth(ref List<CursorData> data, int iteration = 1, float alpha = .33f)
    {
        for (int j = 0; j < iteration; ++j)
        {
            for (int i = 1; i < data.Count - 1; ++i)
            {
                CursorData newCursor = data[i];
                newCursor.pointerPos = (data[i - 1].pointerPos + data[i + 1].pointerPos) * 0.5f * alpha + (1 - alpha) * data[i].pointerPos;
                data[i] = newCursor;
            }
        }
    }

    public void CatmullRomSplineSmooth(ref List<CursorData> data, int numPointsBetween = 1, float minDistance = 0.1f)
    {
        if (data.Count < 4)
        {
            throw new ArgumentException("Need at least four points for Catmull-Rom spline.");
        }

        List<CursorData> smoothedData = new List<CursorData>();

        for (int i = 1; i < data.Count - 2; i++)
        {
            CursorData p0 = data[i - 1];
            CursorData p1 = data[i];
            CursorData p2 = data[i + 1];
            CursorData p3 = data[i + 2];

            smoothedData.Add(p1); // Add the original point

            Vector3 lastPoint = p1.pointerPos;

            for (int j = 1; j <= numPointsBetween; j++)
            {
                float t = j / (float)(numPointsBetween + 1);
                Vector3 newPoint = CatmullRom(p0.pointerPos, p1.pointerPos, p2.pointerPos, p3.pointerPos, t);

                if (Vector3.Distance(lastPoint, newPoint) >= minDistance)
                {
                    CursorData newCursor = new CursorData();
                    newCursor.pointerPos = newPoint;
                    smoothedData.Add(newCursor);
                    lastPoint = newPoint;
                }
            }
        }

        smoothedData.Add(data[data.Count - 2]); // Add the second last point
        smoothedData.Add(data[data.Count - 1]); // Add the last point

        data = smoothedData;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }



    public void Process(ref List<CursorData> data)
    {
        if (data == null || data.Count <= 1) return;

        List<CursorData> resultData = new List<CursorData>();
        List<CursorData> tmpData = new List<CursorData>();
        // This is to prevent different stroke to connect to each other
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].pointerPos.w == 0.5f)
            {
                if (tmpData.Count > 0)
                {
                    Combine_Step(ref tmpData);
                    Combine_Angle(ref tmpData);
                    //Smooth(ref tmpData, 3);
                    CatmullRomSplineSmooth(ref tmpData, 1, 0.1f);
                    for (int j = 0; j < tmpData.Count; ++j)
                    {
                        resultData.Add(tmpData[j]);
                    }
                    tmpData.Clear();
                }
                tmpData.Add(data[i]);
            }
            else
            {
                tmpData.Add(data[i]);
            }
        }
        if (tmpData.Count > 0)
        {
            Combine_Step(ref tmpData);
            //Smooth(ref tmpData, 3);
            CatmullRomSplineSmooth(ref tmpData, 1, 0.1f);

            for (int j = 0; j < tmpData.Count; ++j)
            {
                resultData.Add(tmpData[j]);
            }
            tmpData.Clear();
        }
        data = resultData;
    }


    float CalculateHausdorffDistance(List<Vector4> lines1, List<Vector4> lines2)
    {
        float maxMinDistance1 = CalculateMaxMinDistance(lines1, lines2);
        float maxMinDistance2 = CalculateMaxMinDistance(lines2, lines1);
        return Mathf.Max(maxMinDistance1, maxMinDistance2);
    }

    float CalculateMaxMinDistance(List<Vector4> sourceLines, List<Vector4> targetLines)
    {
        float maxMinDistance = 0;
        foreach (Vector4 sourceLine in sourceLines)
        {
            float minDistance = float.MaxValue;
            foreach (Vector4 targetLine in targetLines)
            {
                float distance = DistanceBetweenLineSegments(sourceLine, targetLine);
                minDistance = Mathf.Min(minDistance, distance);
            }
            maxMinDistance = Mathf.Max(maxMinDistance, minDistance);
        }
        return maxMinDistance;
    }

    float CalculateMeanMinimumDistance(List<Vector4> lines1, List<Vector4> lines2)
    {
        float totalMinDistance = 0;
        int count = 0;

        foreach (Vector4 line1 in lines1)
        {
            float minDistance = float.MaxValue;
            foreach (Vector4 line2 in lines2)
            {
                float distance = DistanceBetweenLineSegments(line1, line2);
                minDistance = Mathf.Min(minDistance, distance);
            }
            totalMinDistance += minDistance;
            count++;
        }

        return totalMinDistance / count;
    }

    float DistanceBetweenLineSegments(Vector4 line1, Vector4 line2)
    {
        // Convert Vector4 into two Vector3s for each line
        Vector3 A = new Vector3(line1.x, line1.y, 0);
        Vector3 B = new Vector3(line1.z, line1.w, 0);
        Vector3 C = new Vector3(line2.x, line2.y, 0);
        Vector3 D = new Vector3(line2.z, line2.w, 0);

        // Simplest case: Distance between the endpoints of each segment
        return Mathf.Min(Vector3.Distance(A, C), Vector3.Distance(B, D));
    }
}
