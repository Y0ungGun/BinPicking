using System.Collections.Generic;
using System;
using Unity.Sentis;
using UnityEngine;
using System.Linq;

public static class NonMaxSuppression
{
    public static List<Tensor<float>> NMS(
    Tensor<float> prediction,
    float confThres = 0.25f,
    float iouThres = 0.45f,
    int maxDet = 300)
    {
        int batchSize = prediction.shape[0];
        int numClasses = prediction.shape[2] - 5;
        List<Tensor<float>> output = new List<Tensor<float>>();

        for (int b = 0; b < batchSize; b++)
        {
            List<BoundingBox> boxes = new List<BoundingBox>();  // BoundingBox 객체 리스트로 수정
            List<float> scores = new List<float>();
            List<int> classes = new List<int>();

            // 필터링: 신뢰도가 confThres 이상인 객체만 남김
            for (int i = 0; i < prediction.shape[1]; i++)
            {
                float conf = prediction[0, i, 4];
                if (conf < confThres) continue;

                float[] box = Enumerable.Range(0, 4).Select(j => prediction[0, i, j]).ToArray();
                int bestClass = ArgMax(prediction, 0, i, 5, numClasses);
                float bestClassScore = prediction[0, i, 5 + bestClass] * conf;

                if (bestClassScore > confThres)
                {
                    // BoundingBox 객체 생성
                    boxes.Add(new BoundingBox(box[0], box[1], box[2], box[3], bestClassScore));
                    scores.Add(bestClassScore);
                    classes.Add(bestClass);
                }
            }

            // NMS 수행
            List<BoundingBox> keep = ApplyNMS(boxes, iouThres);

            // NMS 후 결과 생성
            Tensor<float> result = CreateResultTensor(keep, boxes, scores, classes);
            output.Add(result);
        }

        return output;
    }
    private static Tensor<float> SliceBatch(Tensor<float> tensor, int batchIndex)
    {
        int rows = tensor.shape[1];
        int cols = tensor.shape[2];
        float[] data = new float[rows * cols];
        for (int i = 0; i < rows; i++)
        {
            Array.Copy(tensor.DownloadToArray(), batchIndex * rows * cols + i * cols, data, i * cols, cols);
        }

        return new Tensor<float>(new TensorShape(rows, cols), data);
    }
    private static int ArgMax(Tensor<float> tensor, int batchIndex, int row, int start, int length)
    {
        int bestIndex = 0;
        float bestValue = float.MinValue;

        // 인덱스를 바탕으로 start부터 length까지의 범위에서 최댓값을 찾음
        for (int i = 0; i < length; i++)
        {
            float value = tensor[batchIndex, row, start + i];
            if (value > bestValue)
            {
                bestValue = value;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public static List<BoundingBox> ApplyNMS(List<BoundingBox> boxes, float iouThreshold = 0.5f)
    {
        // 확신도가 높은 순서대로 정렬
        boxes.Sort((a, b) => b.Score.CompareTo(a.Score));

        List<BoundingBox> result = new List<BoundingBox>();
        bool[] suppressed = new bool[boxes.Count];

        for (int i = 0; i < boxes.Count; i++)
        {
            if (suppressed[i])
                continue;

            result.Add(boxes[i]);

            for (int j = i + 1; j < boxes.Count; j++)
            {
                if (suppressed[j])
                    continue;

                // IOU (Intersection over Union) 계산
                float iou = IoU(boxes[i], boxes[j]);

                // IOU가 임계값을 초과하면 두 번째 박스를 억제
                if (iou > iouThreshold)
                {
                    suppressed[j] = true;
                }
            }
        }

        return result;
    }

    private static float IoU(BoundingBox box1, BoundingBox box2)
    {
        float x1 = Mathf.Max(box1.XMin, box2.XMin);
        float y1 = Mathf.Max(box1.YMin, box2.YMin);
        float x2 = Mathf.Min(box1.XMax, box2.XMax);
        float y2 = Mathf.Min(box1.YMax, box2.YMax);

        float interArea = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
        float box1Area = (box1.XMax - box1.XMin) * (box1.YMax - box1.YMin);
        float box2Area = (box2.XMax - box2.XMin) * (box2.YMax - box2.YMin);

        float iou = interArea / (box1Area + box2Area - interArea);
        return iou;
    }
    // Function to perform NMS
    public static Tensor NMS(Tensor boxes, Tensor scores, float iouThreshold)
    {
        // Implement Non-Maximum Suppression (NMS) logic
        // Assuming we use some pre-built NMS function or you can implement your own logic
        return boxes;  // Placeholder for NMS function
    }
    public static Tensor<float> CreateResultTensor(
    List<BoundingBox> keep,
    List<BoundingBox> boxes,
    List<float> scores,
    List<int> classes)
    {
        // 결과 데이터를 담을 List를 초기화
        List<float[]> resultList = new List<float[]>();

        // keep 리스트에서 NMS를 거친 BoundingBox 데이터를 처리
        foreach (var box in keep)
        {
            // [xmin, ymin, xmax, ymax, score] 형식으로 저장
            resultList.Add(new float[] { box.XMin, box.YMin, box.XMax, box.YMax, box.Score });
        }

        // Tensor의 크기를 설정: [num_boxes, 5] 형식으로 설정
        int numBoxes = resultList.Count;
        int tensorSize = numBoxes * 5;

        // 텐서를 만들기 위한 데이터 배열 초기화
        float[] resultArray = new float[tensorSize];

        // resultList에서 값을 배열로 변환하여 resultArray에 채움
        for (int i = 0; i < numBoxes; i++)
        {
            var boxData = resultList[i];
            resultArray[i * 5] = boxData[0]; // xmin
            resultArray[i * 5 + 1] = boxData[1]; // ymin
            resultArray[i * 5 + 2] = boxData[2]; // xmax
            resultArray[i * 5 + 3] = boxData[3]; // ymax
            resultArray[i * 5 + 4] = boxData[4]; // score
        }

        // 텐서로 변환하여 반환
        var resultTensor = new Tensor<float>(new TensorShape(numBoxes, 5), resultArray);
        return resultTensor;
    }

    // Function to convert from xywh to xyxy
    public static Tensor xywh2xyxy(Tensor<float> x)
    {
        int batchSize = x.shape[0];
        float[] y = new float[batchSize * 4];
        for (int i = 0; i < batchSize; i++)
        {
            float cx = x[i, 0]; // center_x
            float cy = x[i, 1]; // center_y
            float w = x[i, 2];  // width
            float h = x[i, 3];  // height

            y[i * 4] = cx - w / 2;      // x1
            y[i * 4 + 1] = cy - h / 2;  // y1
            y[i * 4 + 2] = cx + w / 2;  // x2
            y[i * 4 + 3] = cy + h / 2;  // y2
        }

        return new Tensor<float>(new TensorShape(batchSize, 4), y);
    }
}
public class BoundingBox
{
    public float XMin, YMin, XMax, YMax, Score;

    public BoundingBox(float xmin, float ymin, float xmax, float ymax, float score)
    {
        XMin = xmin;
        YMin = ymin;
        XMax = xmax;
        YMax = ymax;
        Score = score;
    }
}