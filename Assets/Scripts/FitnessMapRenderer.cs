using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitnessMapRenderer : MonoBehaviour
{

    public GridOptimizationProblem problem;
    FitnessMap map;

    public Vector2 gridOrigin;
    public Vector2 cellDimensions;
    public float cellSpacing;

    public GameObject cellRepresentation;

    public Color globalMinColor;
    public Color globalMaxColor;




    // Start is called before the first frame update
    void Start()
    {
        map = problem.optimizationFunction;

        StartCoroutine(progressiveRendering());
     

    }


    IEnumerator progressiveRendering()
    {

        float globalMax = map.MaxVal;
        float globalMin = map.MinVal;

        Debug.Log("fMax = " + globalMax);
        Debug.Log("fMin = " + globalMin);
        
        uint rowCellsPending = map.Width;
        Vector2 conceptMapCoords = new Vector2(0, 0);
        for (float cellX = gridOrigin.x; rowCellsPending > 0; cellX += Mathf.Abs(cellDimensions.x) + cellSpacing)
        {
            uint colCellsPending = map.Height;
            for (float cellY = gridOrigin.y; colCellsPending > 0; cellY += Mathf.Abs(cellDimensions.y) + cellSpacing)
            {

                GameObject nuObj = Instantiate(cellRepresentation, new Vector3(cellX, cellY, 0), Quaternion.identity, transform);

                float thisFitness = map.getFitnessAt((uint)conceptMapCoords.x, (uint)conceptMapCoords.y);

                float heatIntensity = (thisFitness - globalMin) / (globalMax - globalMin);
                //Debug.Log("Heat Intensity: " + heatIntensity);
                if( thisFitness == globalMin)
                {
                    nuObj.GetComponent<MeshRenderer>().material.SetColor("_Color", globalMinColor);
                    nuObj.transform.localScale = new Vector3(cellDimensions.x, cellDimensions.y, 0.01f);

                }
                else if( thisFitness == globalMax)
                {
                    nuObj.GetComponent<MeshRenderer>().material.SetColor("_Color", globalMaxColor);
                    nuObj.transform.localScale = new Vector3(cellDimensions.x, cellDimensions.y, 3);
                }
                else
                {
                    nuObj.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.Lerp(Color.red, Color.blue, 1 - heatIntensity));
                    //Debug.Log("Rendering cell (" + conceptMapCoords.x+ ","+ conceptMapCoords.y +")");
                    nuObj.transform.localScale = new Vector3(cellDimensions.x, cellDimensions.y, 2 * (float)(Mathf.RoundToInt(thisFitness - globalMin)) / (globalMax - globalMin));
                }

                
                
                nuObj.GetComponentInChildren<TextMesh>().text =  thisFitness.ToString();
                nuObj.SetActive(true);

                colCellsPending--;
                conceptMapCoords += new Vector2(0, 1);
                yield return new WaitForSeconds(.1f);
                

            }

            rowCellsPending--;
            conceptMapCoords = new Vector2(conceptMapCoords.x+1, 0);
            yield return new WaitForSeconds(1f);
        }



        yield return null;
    }


    // Update is called once per frame
    void Update()
    {

        

    }
}
