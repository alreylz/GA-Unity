using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GAOptimizationGUI : MonoBehaviour
{

    public GridOptimizationProblem problem;

    public TextMeshProUGUI globalMinMaxValues_Field;
    public TextMeshProUGUI goalOfOptimization_Field;
    public TextMeshProUGUI iterationNum_Field;
    public TextMeshProUGUI status_Field;
    public TextMeshProUGUI gridDimensions_Field;
    public TextMeshProUGUI BestKnown_Field;

    public float updateInterval = 1f;


    void Start()
    {
        StartCoroutine(UpdateRegularly(updateInterval));

    }


    private IEnumerator UpdateRegularly(float period) {

        yield return new WaitForSeconds(1f);

        
            globalMinMaxValues_Field.SetText("Global Max / Min: " + problem.optimizationFunction.MaxVal + "/" + problem.optimizationFunction.MinVal);
            gridDimensions_Field.SetText("DIMENSIONS: " + (problem.optimizationFunction.Width * problem.optimizationFunction.Height).ToString() + "cells");
            goalOfOptimization_Field.SetText("GOAL:" + ((problem.goal == OptimizationType.MINIMIZE) ? "MINIMIZE" : "MAXIMIZE"));

        while (true)
        {
            iterationNum_Field.SetText("Current Iteration: " + problem.iteration);
            status_Field.SetText("GA Status:" + ((problem.bestSolution == problem.optimizationFunction.MaxVal || problem.bestSolution == problem.optimizationFunction.MinVal) ? "OPTIMUM FOUND" : " SEARCHING ..."));
            BestKnown_Field.SetText("Best Known Solution: " + problem.optimizationFunction.getFitnessOfIndividual(problem.bestIndividual).ToString() + "\n\t\t at (" + problem.bestIndividual.x + "," + problem.bestIndividual.y + ")");
            yield return new WaitForSeconds(period);
        }

    }

 
    


}
