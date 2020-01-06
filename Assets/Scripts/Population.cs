using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

//[System.Serializable]
public enum OptimizationType
{
    MINIMIZE,
    MAXIMIZE
}



public enum PopulationEvolutionScheme
{
    Generational, /* Se reemplaza toda la población tras la recombinación y mutación */
    /*Estacionaria: se mantiene parte de la población anterior para la siguiente generación */
    WorstN_Replace_Stationary, /* --> Se reemplazan los N peores */
    Parent_Replace_Stationary, /* --> La nueva población reemplaza a sus padres */
    Similarity_Replace_Stationary, /* --> Elemento reemplaza a aquel cuya distancia de hamming es menor */
    Pure_Stationary /* --> Se reemplaza a un único individuo (el peor) */
}



[System.Serializable]
public class Population
{
    public int PopulationSize { get; private set; } // número de genotipos / cromosomas / coordenadas diferentes manejadas para el AG

    /*Fitness Cumulativo, utilizado para el cálculo de las probabilidades de elección proporcionales al fitness */
    public float CumulativeFitness
    {
        get
        {
            float cumulativeFitness = 0f;
            foreach (var indiv in popIndividuals)
            {
                cumulativeFitness += optimizationFunction.getFitnessOfIndividual(indiv);
            };
            return (cumulativeFitness == 0) ? -1f : cumulativeFitness;
        }
    }
    public float CumulativeFitnessInvProp
    {
        get
        {
            float cumulativeFitness = 0f;
            foreach (var indiv in popIndividuals)
            {
                cumulativeFitness += optimizationFunction.getFitnessOfIndividual(indiv,true);
            };
            return (cumulativeFitness == 0) ? -1f : cumulativeFitness;
        }
    }


    /* Estado del algoritmo, obtiene el punto que posee el valor de fitness óptimo conocido */
    private Individual _PrevBest;
    private Individual _best;
    public Individual BestIndividual {
        get {
            if (_best != null && _PrevBest !=null && _PrevBest == _best) return _best;
            else {
                Individual updatedBest = null;
                #region Naïve Implementation [DELETE LATER]
                /*
                 * float bestFitness = float.PositiveInfinity;
                 * foreach (var i in popIndividuals)
                {
                    if (optimizationFunction.getFitnessOfIndividual(i) < bestFitness)
                    {
                        updatedBest = i;
                    }
                }
                */
                #endregion Naïve implementation
                if(optimizationGoal == OptimizationType.MINIMIZE) updatedBest = IndivRanking.Values[0][0];
                else updatedBest = IndivRanking.Values[IndivRanking.Values.Count-1][0];
                _PrevBest = _best;
                _best = updatedBest;
                return updatedBest;
        };
        }
    }
    /* Obtiene el valor de fitness más óptimo conocido */
    public float BestSolution
    {
        get
        {
            if (optimizationGoal == OptimizationType.MINIMIZE) return IndivRanking.Keys[0];
            else return IndivRanking.Keys[IndivRanking.Keys.Count - 1];
        }

    }

    /*Exploración de la lista ordenada de individuos de la población, permitiendo obtener elementos el la posición N */
    public Individual getAtNthBestIndividual(int index)
    {
        if (index >= IndivRanking.Count) return null;
        return IndivRanking.Values[index][0];
    }
    public List<Individual> getAtNthBestIndividuals(int index)
    {
        if (index >= IndivRanking.Count) return null;
        return IndivRanking.Values[index];
    }
    public float getNthBestFitness(int index)
    {
        if (index >= IndivRanking.Count) return float.PositiveInfinity;
        return IndivRanking.Keys[index];
    }

    /* Estructuras de datos de individuos */
    SortedList<float, List<Individual>> IndivRanking;
    private Individual[] popIndividuals;

    /*Parámetros de configuración de la población */

    private float mutationFactor = 0.5f; // 0--> No se produce mutación; 0.5 --> La mitad de los bits pueden mutar
    FitnessMap optimizationFunction; // Función a optimizar; dada por un grid de Fitness conocido
    OptimizationType optimizationGoal; // MINIMIZE o MAXIMIZE
    
    //Step 0: Problem input
    public Population(FitnessMap optimProblem, OptimizationType goalType = OptimizationType.MINIMIZE)
    {
        optimizationFunction = optimProblem;
        optimizationGoal = goalType;
    }
    
    //Step 1: Initialization of chromosomes (each gene is a 0 or a 1; a bit)
    public bool InitPopulation(int popSize, float mutationFactor,  int rndSeed=2)
    {
        if (popSize > optimizationFunction.Width * optimizationFunction.Height || popSize <= 0)
        {
            PopulationSize = 0;
            return false;
        }
        if (mutationFactor < 0 || mutationFactor > 1) { Debug.LogException(new System.Exception("Invalid Mutation Factor")); return false; }

        this.mutationFactor = mutationFactor;
        PopulationSize = popSize;

        //Creación de individuos de forma aleatoria dentro del Grid del problema
        popIndividuals = new Individual[PopulationSize];
        IndivRanking = new SortedList<float, List<Individual>>();
        for (int i = 0; i< PopulationSize; i++)
        {
            popIndividuals[i] = new Individual("i" + (i + 1), optimizationFunction.Width, optimizationFunction.Height);
           
            if (!IndivRanking.ContainsKey(optimizationFunction.getFitnessOfIndividual(popIndividuals[i])))
                IndivRanking.Add(optimizationFunction.getFitnessOfIndividual(popIndividuals[i]), new List<Individual> { popIndividuals[i] });
            else
            {
                List<Individual> inds;
                IndivRanking.TryGetValue(optimizationFunction.getFitnessOfIndividual(popIndividuals[i]), out inds);
                inds.Add(popIndividuals[i]);
            }   
        }
        return true;
    }

    #region Implementation of Selection Schemes 
    /* Selección de los N mejores Individuos como padres */
    public List<Individual> ElitistSelection( uint numTopIndivSelected  ) {

        List<Individual> selectedChromosomes = new List<Individual>();
        for (int i = 0; i < numTopIndivSelected; )
        {
            foreach (var sameFitIt in IndivRanking.Values[i])
            {
                selectedChromosomes.Add(sameFitIt);
                i++;
            }
        }

        return selectedChromosomes;
    }
    /* Selección de M individuos como padres, seleccionados con probabilidad proporcional al fitness */
    public List<Individual> RouletteSelection( uint numToChoose, out HashSet<Individual> nonRepeatedParents)
    {
        if(numToChoose > PopulationSize) { Debug.LogException(new System.Exception("Number of parents for next generation cannot exceed Population Size")); nonRepeatedParents = null;  return null; }

        //Cálculo probabilidad de elección de cromosomas (padres de nuevas generaciones) proporcional al fitness de los individuos existentes
        float[] selProbability = new float[PopulationSize];
        int rouletteSegment = 0;
        foreach (var i in popIndividuals)
        {
            if(optimizationGoal == OptimizationType.MAXIMIZE) selProbability[rouletteSegment] = optimizationFunction.getFitnessOfIndividual(i) / CumulativeFitness;
            else if(optimizationGoal == OptimizationType.MINIMIZE) selProbability[rouletteSegment] = optimizationFunction.getFitnessOfIndividual(i,true) / CumulativeFitnessInvProp;
            //Debug.Log(i + " RSegment[rouletteSegment]" + " f(this)=" + optimizationFunction.getFitnessOfIndividual(i)+" Psel ="+ selProbability[rouletteSegment].ToString());
            rouletteSegment++;
        }

        //Conversión de probabilidades a un segmento con intervalos ordenados de tamaño proporcional a la probabilidad de selección
        rouletteSegment = 0;
        float cumulativeProb = 0;

        float[] cumulativeSegment = new float[PopulationSize]; //index es el id del segmento; contenido es el upper bound del intervalo (e.g. [0,0.2) --> 0.2 )
        foreach (var i in selProbability)
        {
            cumulativeSegment[rouletteSegment] = cumulativeProb + selProbability[rouletteSegment];
            cumulativeProb += selProbability[rouletteSegment++];
        }



        if (cumulativeProb > 1+0.1f) Debug.LogError("Sum of segment assigned probabilites is > 1 ==> Imposible");
        
        //Generación de un número de valores [0,1] igual a un número deseado de padres (normalmente al tamaño de la población)
        //obtenemos así un subconjunto de individuos como padres para la siguiente generación
        List<Individual> selectedChromosomes = new List<Individual>();
        nonRepeatedParents = new HashSet<Individual>();
        float[] _AUX_rndValsPopulationSize = new float[PopulationSize];
        for (int valCounter = 0; valCounter < numToChoose; valCounter++)
        {
            //nuevo valor aleatorio [0,1]
            float val = Random.value;
            _AUX_rndValsPopulationSize[valCounter] = val;
            //Obtenemos el índice correspondiente al individuo asociado al intervalo en el que cae el nuevo valor
            int indexAssociatedToCorrespondingIndiv = 0;
            for (int segmentCheckIt = 0; segmentCheckIt < PopulationSize; segmentCheckIt++)
            {
                if (val >= cumulativeSegment[indexAssociatedToCorrespondingIndiv]) indexAssociatedToCorrespondingIndiv++;
                else break;
            }
            selectedChromosomes.Add(popIndividuals[indexAssociatedToCorrespondingIndiv]);
            nonRepeatedParents.Add(popIndividuals[indexAssociatedToCorrespondingIndiv]);
        }
        
        return selectedChromosomes;
    }
    /* Selección de M individuos como padres cuya probabilidad de selección depende de su posición en el ranking de fitness */
    public List<Individual> RankBasedRouletteSelection(uint numTopNtoConsider, uint numParentsToObtain, out HashSet<Individual> nonRepeatedParents) {

        if (numTopNtoConsider > PopulationSize) { Debug.LogException(new System.Exception("Roulette Rank too High ")); nonRepeatedParents = null; return null; }
        uint cumulativeRank = 0;

        List<Individual> selectedChromosomes = new List<Individual>();
        nonRepeatedParents = new HashSet<Individual>();
        for (uint i = numTopNtoConsider; i > 0; i--)
        {
            int numDraws = getAtNthBestIndividuals((int) i).Count;
            cumulativeRank += (uint) numDraws *i;
        }
        
        //Asigno intervalos de en el rango [0,1] de tamaño proporcional a la posición en el ranking
        float[] probSegmentUpperBounds = new float[numTopNtoConsider];
        float acc = 0f;
        for (int i=1 ; i<=numTopNtoConsider; i++)
        {

            if( optimizationGoal == OptimizationType.MINIMIZE) probSegmentUpperBounds[i-1] = acc + ((float) 1f/(float) i /  (float)cumulativeRank);
            else probSegmentUpperBounds[i - 1] = acc + ( (float)i / (float)cumulativeRank);

            acc += probSegmentUpperBounds[i-1];
        }

        //TEST [DELETE ONCE TESTED]
        if (optimizationGoal == OptimizationType.MINIMIZE && probSegmentUpperBounds[1] - probSegmentUpperBounds[0] > probSegmentUpperBounds[0]) Debug.LogError("SHIT 1, PROBABILITIES ARE NOT PROPERLY COMPUTED");
        else if (optimizationGoal == OptimizationType.MAXIMIZE && probSegmentUpperBounds[1] - probSegmentUpperBounds[0] < probSegmentUpperBounds[0]) Debug.LogError("SHIT 2,  PROBABILITIES ARE NOT PROPERLY COMPUTED");

        //Recorro los diversos intervalos para ver cuál en cuál cae el número aleatorio generado
        float randVal;
        for (uint n = numParentsToObtain; n > 0; n--)
        {
            randVal = Random.value;
            for (int i = 0; i < numTopNtoConsider; i++)
            {
                if (randVal >= probSegmentUpperBounds[i]) continue;
                else
                {
                    int rndForSameFitnessRouletteIntervals = Random.Range(0, IndivRanking.Values[i].Count);
                    selectedChromosomes.Add(IndivRanking.Values[i][rndForSameFitnessRouletteIntervals]);
                    nonRepeatedParents.Add(IndivRanking.Values[i][rndForSameFitnessRouletteIntervals]);
                    break;
                }
            }
        }

        return selectedChromosomes;

    }
    /* Stochastic Universal Sampling Selection: se extraen exactamente nParents a través de la generación de un valor único aleatorio 
    /* y recorrer el segmento de probabilidad proporcional en saltos constantes dados por 1/nParentsToChoose */
    public List<Individual> SUSSelection(uint nParentsToChoose, out HashSet<Individual> nonRepeatedParents )
    {
        
        //Cálculo probabilidad de elección de cromosomas (padres de nuevas generaciones) proporcional al fitness de los individuos existentes
        float[] selProbability = new float[PopulationSize];
        int rouletteSegment = 0;
        foreach (var i in popIndividuals)
        {
            if (optimizationGoal == OptimizationType.MINIMIZE)  selProbability[rouletteSegment] = optimizationFunction.getFitnessOfIndividual(i,true) / CumulativeFitnessInvProp;
            else selProbability[rouletteSegment] = optimizationFunction.getFitnessOfIndividual(i) / CumulativeFitness;
            rouletteSegment++;
        }
        //Conversión de probabilidades a un segmento con intervalos ordenados de tamaño proporcional a la probabilidad de selección
        rouletteSegment = 0;
        float cumulativeProb = 0;

        float[] cumulativeSegment = new float[PopulationSize]; //index es el id del segmento; contenido es el upper bound del intervalo (e.g. [0,0.2) --> 0.2 )
        foreach (var i in selProbability)
        {
            cumulativeSegment[rouletteSegment] = cumulativeProb + selProbability[rouletteSegment];
            cumulativeProb += selProbability[rouletteSegment++];
        }
        //Generación de un número de valores [0,1] igual al tamaño de la población
        //obtenemos así un subconjunto de individuos como padres para la siguiente generación
        List<Individual> selectedChromosomes = new List<Individual>();
        nonRepeatedParents = new HashSet<Individual>();
        float stepLength = 1.0f / nParentsToChoose;

        float val = Random.Range(0, stepLength);
        //Obtenemos el índice correspondiente al individuo asociado al intervalo en el que cae el nuevo valor
        
        for (float stepVal = val ; stepVal <= 1 ; stepVal += stepLength) { 
            int indexAssociatedToCorrespondingIndiv = 0;
            for (int step = 0; step < PopulationSize; step++)
                {
                    if (stepVal >= cumulativeSegment[indexAssociatedToCorrespondingIndiv]) indexAssociatedToCorrespondingIndiv++;
                    else break;
                }
            selectedChromosomes.Add(popIndividuals[indexAssociatedToCorrespondingIndiv]);
            nonRepeatedParents.Add(popIndividuals[indexAssociatedToCorrespondingIndiv]);
        }

        return selectedChromosomes;
    }
    /*Tournament Selection: Seleccionamos aleatoriamente n individuos, formamos un grupo y escogemos el que mejor fitness tenga; 
     * repetimos hasta obtener el número de padres deseados */
    public List<Individual> KWayTournamentSelection(uint groupSize, uint numParentsToChoose, out HashSet<Individual> nonRepeatedParents)
    {
        if(groupSize == 0 || groupSize > PopulationSize) { Debug.LogException(new System.Exception("Group size is too large")); nonRepeatedParents = null; return null; }

        
        List<Individual> selectedChromosomes = new List<Individual>();
        nonRepeatedParents = new HashSet<Individual>();

        //Creamos un grupo en una lista ordenada por fitness
        SortedList<float, Individual> group = new SortedList<float, Individual>();
        for (int gi = 0; gi < numParentsToChoose; gi++) { 
            for (int i = 0; i < groupSize;)
            {
                int rndIndex = Random.Range(0, PopulationSize);
                if(!group.ContainsKey(optimizationFunction.getFitnessOfIndividual(popIndividuals[rndIndex])))
                    group.Add(optimizationFunction.getFitnessOfIndividual(popIndividuals[rndIndex]), popIndividuals[rndIndex]);
                if (group.Count == i + 1) i++;
            }
            //Select Best
            selectedChromosomes.Add(group.Values[0]);
            nonRepeatedParents.Add(group.Values[0]);
            group.Clear();
        }

        return selectedChromosomes;
    }
    #endregion End Implementation of Selection Schemes

    /* Crea parejas para reproducción */
    public Individual[] DoMatchMaking(List<Individual> parents)
    {
        Individual[] couple = new Individual[2];
        couple[0] = parents[Random.Range(0, parents.Count)];
        do
        {
            couple[1] = parents[Random.Range(0, parents.Count)];
        } while (couple[1] == couple[0]);

        return couple;
    }
    
  
    /* Cada gen es tratado de forma independiente y se selecciona el gen de un padre u otro de forma aleatoria para generar el cromosoma resultante */
    public List<Individual> UniformCrossOver(List<Individual> allParentsForRecombination, uint numOffspring)
    {
        Dictionary<string,Individual> children = new Dictionary<string, Individual>();

        Debug.Log("I ENTERED CROSSOVER");


        for (int n = 0; n < numOffspring;) { //Generamos numOffspring hijos
            Individual[] thisOffspringParents = DoMatchMaking(allParentsForRecombination); //Obtenemos una pareja

            char [] progenitorA = thisOffspringParents[0].Gray.ToCharArray();
            char [] progenitorB = thisOffspringParents[1].Gray.ToCharArray();

            Debug.Log("Mating complete: Couple is : \n" + thisOffspringParents[0].Gray + "\n" + thisOffspringParents[1].Gray);

            StringBuilder debugChromosome = new StringBuilder();

            StringBuilder nuChromosome = new StringBuilder();
            nuChromosome.EnsureCapacity((int)thisOffspringParents[0].ChromosomeLength);
            for (int geneIterator = 0;  geneIterator < thisOffspringParents[0].ChromosomeLength; geneIterator++)
            {
                //Elegimos de que padre el gen ith se transmitirá a la descendencia
                int chosenParent = Random.Range(0, 2);

                if (chosenParent == 0) { nuChromosome.Append(progenitorA[geneIterator]); debugChromosome.Append("P0"); }
                else { nuChromosome.Append(progenitorB[geneIterator]); debugChromosome.Append("P1");}

            }
            Debug.Log("Resulting child: \n" + debugChromosome + "\n" + nuChromosome);
            
            //Añadimos elementos y creamos nuevo cromosoma, solo si no existe lo contamos
            if (!children.ContainsKey(nuChromosome.ToString())) {
                children.Add(nuChromosome.ToString(), new Individual(Random.Range(0, 50).ToString(), nuChromosome.ToString(), optimizationFunction.Width, optimizationFunction.Height));
                n++;
            }

        }

        Debug.Log("I EXIT CROSSOVER");

        return new List<Individual> (children.Values);

    }


    //[PENDING SOMEDAY]
    /* Genera dos hijos por cada par de padres cada uno combinando los cromosomas realizando un corte por un punto */
    //public void OnePointCrossOver() { }
    /* Se definen varios puntos de corte y se generan hijos dado el número de combinaciones resultante */
    //public void MultiPointCrossOver() { }

    public List<Individual> Mutation(List<Individual> toMutate)
    {
        foreach(var i in toMutate)
        {
            i.DoMutation(Mathf.FloorToInt(mutationFactor * i.ChromosomeLength));

        }
        return toMutate;
    }


    public void ReplaceN_Worst(int numToReplace , List<Individual>  nuIndividuals ){


        //Cojo el peor --> saco los Individuos de su lista  y los cuento
        //Elimino e introduzco tantos como he eliminado

        int pendingToReplace = numToReplace; // Hasta que todos los necesarios hayan sido reemplazados

        for (int inverselistIteration = (IndivRanking.Count - 1); pendingToReplace > 0; inverselistIteration--)
        {
            //Elementos con peor evaluación 
            List<Individual> sameFitnessElems = IndivRanking.Values[inverselistIteration];

            if (pendingToReplace >= sameFitnessElems.Count)
            {

                IndivRanking.RemoveAt(inverselistIteration); //Elimino todos los elementos de la lista vieja
                for (int i = 0; i < sameFitnessElems.Count; i++) //Incluyo tantos nuevos como he eliminado
                {
                    float fitnessNuevoIndividuo = optimizationFunction.getFitnessOfIndividual(nuIndividuals[0]);
                    if (IndivRanking.ContainsKey(fitnessNuevoIndividuo)) // Si ya exxiste un individuo con el valor, añadimos a la lista de individuos con tal valor
                    {
                        IndivRanking[fitnessNuevoIndividuo].Add(nuIndividuals[0]);
                    }
                    else IndivRanking.Add(fitnessNuevoIndividuo, new List<Individual> { nuIndividuals[0] });
                    nuIndividuals.RemoveAt(0);
                    pendingToReplace--;
                }
            }
            else
            { //Delete pendingToReplace elements

                for (int u = 0; u < pendingToReplace; u++)
                {
                    List<Individual> delPending = IndivRanking[inverselistIteration];
                    delPending.RemoveAt(0); //Elimino uno a uno hasta lograr el reemplazo de todos los pendientes
                                            //añado uno de la lista nueva
                    float fitnessNuevoIndividuo = optimizationFunction.getFitnessOfIndividual(nuIndividuals[0]);
                    if (IndivRanking.ContainsKey(fitnessNuevoIndividuo)) // Si ya exxiste un individuo con el valor, añadimos a la lista de individuos con tal valor
                    {
                        IndivRanking[fitnessNuevoIndividuo].Add(nuIndividuals[0]);
                    }
                    else IndivRanking.Add(fitnessNuevoIndividuo, new List<Individual> { nuIndividuals[0] });
                    nuIndividuals.RemoveAt(0);
                    pendingToReplace--;
                }
            }
            
        }
        //Resetear y Cargar los elementos desde la lista ordenada con la que hemos trabajado para el reemplazo
        popIndividuals = new Individual[PopulationSize];

        int itRefillArray = 0;
        //RELLENAMOS EL ARRAY CONVENIONAL CON la lista ordenada
        foreach(var listIndiv in IndivRanking.Values)
        {
            foreach(Individual indiv in listIndiv) {
                popIndividuals[itRefillArray] = indiv;
                itRefillArray++;
            }
        }

        if (popIndividuals.Length != PopulationSize) Debug.LogException(new System.Exception("ERROR WITH REPLACE N --> RESULTING POPULATION IS NOT THE SAME AS THE ORIGINAL"));

        

    }





    public void ClearUpdateRanking()
    {
        IndivRanking.Clear();
        foreach (var p in popIndividuals)
        {
            if (!IndivRanking.ContainsKey(optimizationFunction.getFitnessOfIndividual(p)))
                IndivRanking.Add(optimizationFunction.getFitnessOfIndividual(p), new List<Individual> { p });
            else
            {
                IndivRanking[IndivRanking.IndexOfKey(optimizationFunction.getFitnessOfIndividual(p))].Add(p);
            }
        }
    }

    public void PrintRanking()
    {
        StringBuilder strB = new StringBuilder();
        int position = 1;
        foreach (var i in IndivRanking)
        {
            strB.AppendLine();
            strB.Append(position + "- ");
            foreach (var j in i.Value)
            { //Para cada elemento que tenga el mismo fitness...
                strB.Append("f(" + j.x + "," + j.y + ") = " + i.Key.ToString() + " ; ");
            }
            position++;
        }

        Debug.Log(strB.ToString());
    }




    public static void PrintSelectedChromosomes(List<Individual> selectedList)
    {
        StringBuilder builder = new StringBuilder("Selected Chromosomes:");
        int index = 1;
        foreach (var chr in selectedList)
        {
            builder.AppendLine("CHROMOSOME[" + index + "]:+ (" + chr.x + "," + chr.y + ")" + "encoded: (" + chr.grayX + ", " + chr.grayY + ")");
            index++;
        }
        Debug.Log(builder.ToString());
    }

    public override string ToString()
    {
        if (PopulationSize == 0 || popIndividuals == null || popIndividuals.Length == 0 )
        {
            return "Population not initialised";
        }
        StringBuilder individualsString = new StringBuilder("Population: { ");
        foreach (var individual in popIndividuals)
        {
            individualsString.Append(individual.ToString());
        }
        individualsString.Append("}");
        return individualsString.ToString();
    }

}
