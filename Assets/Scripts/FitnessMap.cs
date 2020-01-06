using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[System.Serializable]
public class FitnessMap
{ 

    float[,] fitnessGrid;

    public uint Height { get => (uint)fitnessGrid.GetLongLength(0); }
    public uint Width { get => (uint)fitnessGrid.GetLongLength(1);  }

    public float MaxVal { get; private set; } = float.NegativeInfinity;
    public float MinVal { get; private set; } = float.PositiveInfinity;


    public FitnessMap(uint width=2000000, uint height=2000000)
    {
        fitnessGrid = new float[height,width];
        for (var row = 0; row < height; row++)
            for (var col = 0; col < width; col++)
            {
                fitnessGrid[row, col] = Random.Range(0f, 10000f);
                if (MaxVal < fitnessGrid[row, col]) MaxVal = fitnessGrid[row, col];
                if (MinVal > fitnessGrid[row, col]) MinVal = fitnessGrid[row, col];
            }
    }
    
    public float getFitnessOfIndividual(Individual individual, bool inverselyProportional = false)
    {
        if (individual.x > Width - 1 || individual.y > Height - 1) { Debug.LogException(new System.IndexOutOfRangeException()); return -1f; }

        if (!inverselyProportional) return fitnessGrid[individual.y, individual.x];
        else return  1f / fitnessGrid[individual.y, individual.x];
    }

    public float getFitnessAt(uint x, uint y, bool inverselyProportional = false )
    {
        if (x > Width - 1 || y > Height - 1) { Debug.LogException(new System.IndexOutOfRangeException()); return -1f; }
        if (!inverselyProportional) return fitnessGrid[y, x];
        else return  1f / fitnessGrid[y, x];
    }

    public override string ToString()
    {
        StringBuilder strBuilder = new StringBuilder("Fitness Map:  { ");
        for(uint row = 0; row < Height; row++)
        {
            for(uint col = 0; col<Width; col++)
            {
                strBuilder.Append( "f("+col+","+row+") :"+getFitnessAt(col, row).ToString()+ ((col!=Width-1)? " ; ":" "));
            }
        }
        strBuilder.Append("}");
        return strBuilder.ToString();
    }


}
