using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using SysRand = System.Random;
using UnityEngine;


[System.Serializable]
public class Individual
{
    /* Parámetros de la función a maximizar */
    public static SysRand rndGen;

    public string Id { get; private set; }
    public uint x;
    public uint y;

    public string grayX;
    public string grayY;
    public string Gray { get => grayX + grayY; }


    public uint maxX;
    public uint maxY;

    private int []   requiredToEncodeBits; // Array que indica cuántos bits son necesarios para codificar cada componente de las coordenadas del individuo
    public uint ChromosomeLength { get { return (uint)Gray.Length; } }


    // Inicializar aleatoriamente (origen considerado en cero)

    public void setRndSeed(int seed) { rndGen = new SysRand(seed); }


    //Generación aleatoria de puntos dado el tamaño del grid
    public Individual(string id, uint maxX, uint maxY)
    {
        Id = id;
        if (rndGen == null) setRndSeed((int)Time.time);

        this.maxX = maxX;
        this.maxY = maxY;

        //Número de bytes necesarios para representar maxX Valores 
        requiredToEncodeBits = new int[2];
        requiredToEncodeBits[0] = Mathf.CeilToInt(Mathf.Log(maxX, 2));
        requiredToEncodeBits[1] = Mathf.CeilToInt(Mathf.Log(maxY, 2));

        Debug.Log("Bits to represent Width " + maxX + " = " + requiredToEncodeBits[0]);
        Debug.Log("Bits to represent Height " + maxY + " = " + requiredToEncodeBits[1]);


        Byte [] xRndBytes = new Byte[ (int) (((requiredToEncodeBits[0] <8) ? 8: requiredToEncodeBits[0]) / 8)  ] ;
       
        Byte[] yRndBytes = new Byte[ (int) ( ((requiredToEncodeBits[1] < 8 ) ? 8 : requiredToEncodeBits[1]) / 8) ];


        Debug.Log("Bytes to represent Width " + maxX + " = " + xRndBytes.Length);
        Debug.Log("Bytes to represent Height " + maxY + " = " + yRndBytes.Length);


        //Generamos los bytes necesarios para calcular coordenadas aleatorias

        do
        {
            //Get as many bytes as necessary to store a value smaller than the maximum
            rndGen.NextBytes(xRndBytes);
            int posIterator = 0;
            //Transformamos array de bytes aleatorios a cada una de las componentes
            foreach (Byte b in xRndBytes)
            {
                x = Convert.ToUInt32(b) << posIterator;
                posIterator++;
            }

        } while (x >= maxX || x < 0);

        do
        {
            rndGen.NextBytes(yRndBytes);
            int posIterator = 0;
            foreach (Byte b in yRndBytes)
            {
                y = Convert.ToUInt32(b) << posIterator;
                posIterator++;
            }
        } while (y >= maxY || y < 0);

        //Transformación a Gray Encoding
        ToBinaryGrayEncoding();


    }

    
    //HERE CHECK ------------------------------------------------------------------



    public Individual(string id, string fromGray, uint maxX, uint maxY)
    {
        Id = id;
        if (rndGen == null) setRndSeed((int)Time.time);

        this.maxX = maxX;
        this.maxY = maxY;
        //Número de bytes necesarios para representar maxX Valores 
        requiredToEncodeBits = new int[2];
        requiredToEncodeBits[0] = Mathf.CeilToInt(Mathf.Log(maxX, 2));
        requiredToEncodeBits[1] = Mathf.CeilToInt(Mathf.Log(maxY, 2));
        
        Byte[] xRndBytes = new Byte[(int)(((requiredToEncodeBits[0] < 8) ? 8 : requiredToEncodeBits[0]) / 8)];

        Byte[] yRndBytes = new Byte[(int)(((requiredToEncodeBits[1] < 8) ? 8 : requiredToEncodeBits[1]) / 8)];


        //MAKE SURE IT IS OK
        grayX = fromGray.Substring(0, requiredToEncodeBits[0]);
        grayY = fromGray.Substring(requiredToEncodeBits[0]);

        x = ToBinary(grayX);
        y = ToBinary(grayY);

    }
    // Helper function to xor 
    // two characters 
    static char xor_c(char a, char b)
    {
        return (a == b) ? '0' : '1';
    }

    // Helper function to flip the bit 
    static char flip(char c)
    {
        return (c == '0') ? '1' : '0';
    }
    private uint ToBinary(string gray)
    {
            string binary = "";

            // MSB of binary code is same 
            // as gray code 
            binary += gray[0];

            // Compute remaining bits 
            for (int i = 1; i < gray.Length; i++)
            {

                // If current bit is 0, 
                // concatenate previous bit 
                if (gray[i] == '0')
                    binary += binary[i - 1];

                // Else, concatenate invert of 
                // previous bit 
                else
                    binary += flip(binary[i - 1]);
            }

            return Convert.ToUInt32(binary,2); //--> FUCKING STRING TO BE CONVERTED TO UINT (MAYBE OK?????????????)


    }



    public void DoMutation(int numMutations)
    {
        if (numMutations == 0) return;

        Debug.Log("num Mutations " + numMutations);

        char[] toMutateArray = Gray.ToCharArray();

        Debug.Log("Gray to mutate: " + Gray);


        for (int i=numMutations; i>0; i--) //Hasta completar el número de mutaciones totales
        {
            int indexToMutate = UnityEngine.Random.Range(0, Gray.Length);
            toMutateArray[indexToMutate] = flip(toMutateArray[indexToMutate]);
        }



        string mutatedAlready = new string(toMutateArray);

        Debug.Log("MUTATED: " + mutatedAlready);


        grayX = mutatedAlready.Substring(0, requiredToEncodeBits[0]);
        grayY = mutatedAlready.Substring(requiredToEncodeBits[0]);

        x = ToBinary(grayX);
        y = ToBinary(grayY);

    }


    //HERE END CHECK ---------------------------------------------------------------


 


    public void ToBinaryGrayEncoding()
    {
        //Transformación a Gray encoding, donde coordenadas cercanas se diferencian en un solo bit    (e.g. 2 = 011, 3 = 010 )se calcula así:
        uint grayX =  x ^ (x >> 1);
        uint grayY = y ^ (y >> 1);
        this.grayX = System.Convert.ToString(grayX, 2);
        this.grayY = System.Convert.ToString(grayY, 2);
        if (this.grayX.Length < requiredToEncodeBits[0]) this.grayX = this.grayX.PadLeft(requiredToEncodeBits[0], '0');
        if (this.grayY.Length < requiredToEncodeBits[1]) this.grayY = this.grayY.PadLeft(requiredToEncodeBits[1], '0');
    }

    public bool InBoundsTest()
    {
        if (x > maxX || y > maxY){Debug.LogException(new System.IndexOutOfRangeException()); return false; }
        return true;
    }


    public int CompareHamming( Individual toCompare)
    {
        if (toCompare.grayX.Length != this.grayX.Length || toCompare.grayY.Length != this.grayY.Length)
        {
            Debug.Log("Hamming distance cannot be calculated over individuals with GrayEncodings of different lengths");
            return -1; 
        }

        int distance =
        toCompare.grayX.ToCharArray().Zip(grayX.ToCharArray(), (c1, c2) => new { c1, c2 })
        .Count(m => m.c1 != m.c2) +
            toCompare.grayY.ToCharArray()
        .Zip(grayY.ToCharArray(), (c1, c2) => new { c1, c2 })
        .Count(m => m.c1 != m.c2);

        return distance; ;
    }

    public override string ToString()
    {
        return "Individual: "+ Id + " at  (x,y)=("+x+","+y+") \n Chromosome length: "+ChromosomeLength +"\n GrayEncoding ["+grayX+"]["+grayY+"]" ;
    }
    





}
