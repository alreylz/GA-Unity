using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridOptimizationProblem :  MonoBehaviour
{
    

    [Header("Function to Optimize")]
    public FitnessMap optimizationFunction;
    public OptimizationType goal;
    public bool generateRuntimeFunction; // TRUE = activa la generación de una nueva función de fitness ; FALSE = Activa la carga desde archivo
    public uint width;
    public uint height;




    [Header("Configuration of the GA")]
    public Population population;
    public int populationSize = 10;
    public int numParentsPerGen = 5;
    [Range(0f, 1f)]
    public float replacementFactor = 1f;
    [Range(0f, 1f)]
    public float mutationFactor = 0.5f;

    public uint endAtIterationMax = 2000;

    public float bestSolution = float.PositiveInfinity;
    public Individual bestIndividual = null;


    public int iteration = 0;

    private void Awake()
    {
        //GENERACIÓN DE FUNCIÓN DE FITNESS : (Grid de HxW) 
        optimizationFunction = new FitnessMap(width, height);
        Debug.Log("<b><color=teal>[1a] FITNESS FUNCTION CREATED </color></b>" + optimizationFunction );

        //Load map [FUTURE]
        //INICIALIZAR LA POBLACIÓN
        population = new Population(optimizationFunction, OptimizationType.MINIMIZE);
        bool initSucess = population.InitPopulation(populationSize, mutationFactor);
        Debug.Log("<b><color=teal>[2] POPULATION INITIALISED (Basic CONFIGURATION + MUTATION FACTOR) </color></b>" + population);






        Debug.Log("Comparing performance with different sizes: mutacion mitad, reemplazo mitad");
        TestWithParams(20, 0.5f, 0.5f, 10);
        //TestWithParams(20, 0.5f, 0.5f, 10 / 2);
        //TestWithParams(30, 0.5f, 0.5f, 10 / 2);
        //TestWithParams(40, 0.5f, 0.5f, 10 / 2);

















    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            DoGAIteration();
        }
    }



    bool DoGAIteration()
    {
        bool foundBest = false;


        uint numberOfReplacements = (uint)Mathf.RoundToInt(replacementFactor * populationSize);

        if (bestIndividual == null) bestIndividual = population.BestIndividual;
        else if(optimizationFunction.getFitnessOfIndividual(population.BestIndividual) < optimizationFunction.getFitnessOfIndividual(bestIndividual))
        {
            bestIndividual = population.BestIndividual;
        }
        bestSolution = optimizationFunction.getFitnessOfIndividual(bestIndividual);

        if (bestSolution == optimizationFunction.MinVal && goal == OptimizationType.MINIMIZE) return foundBest=true;


        //Selection: devuelve un conjunto de padres para la creación de nuevos individuos
        HashSet<Individual> nonRepParents; 
        List<Individual> nextGenParents = population.RouletteSelection((uint) numParentsPerGen, out nonRepParents);
        Debug.Log("<b><color=teal>[3] SELECTION DONE </color></b>"); Population.PrintSelectedChromosomes(nextGenParents);
        //Crossover: creación de descendencia
        List<Individual> nextGenChildren = population.UniformCrossOver(nextGenParents, numberOfReplacements);
        Debug.Log("<b><color=teal>[4] CROSSOVER DONE </color></b>"); Population.PrintSelectedChromosomes(nextGenChildren);
        //Mutation: mutación de algunos bits
        population.Mutation(nextGenChildren);
        Debug.Log("<b><color=teal>[5] MUTATION DONE </color></b>"); Population.PrintSelectedChromosomes(nextGenChildren);

        if (numberOfReplacements != nextGenChildren.Count) Debug.LogException(new System.Exception("TO REPLACE NOT THE SAME AS DELETED ELEMENTS"));
        Debug.Log("NUMBER OF REPLACEMENTS TO PERFORM " + numberOfReplacements);
        Debug.Log("NUMBER OF CHILDREN BEING GENERATED FOR NEXT ITERATION" + nextGenChildren.Count);
        
        //Reemplazo y nueva iteración
        population.ReplaceN_Worst((int)numberOfReplacements, nextGenChildren);

        iteration++;

        return foundBest;

    }



    public void TestWithParams( int numIndiv, float mutationFactor, float replacementFactor, int numParentsPergen)
    {
        //INIT
        population = new Population(optimizationFunction, OptimizationType.MINIMIZE);
        bool initSucess = population.InitPopulation(populationSize, mutationFactor);
        
        populationSize = numIndiv;
        this.mutationFactor = mutationFactor;
        this.replacementFactor = replacementFactor;
        numParentsPerGen = numParentsPergen;

        iteration = 0;
        bool foundPrematureSolution = false;
        while (iteration < endAtIterationMax)
        {
            if (DoGAIteration()) { foundPrematureSolution = true; break; }
        }
        Debug.Log(" TEST: \n " +
            "Population size = " + populationSize + "\n" +
            "Mutation factor  = " + mutationFactor + "\n" +
            "Replacement factor  = " + replacementFactor + "\n" +
            "Number of parents per gen  = " + numParentsPergen + "\n\n" +
            "STATS: \n\n" +
            ((foundPrematureSolution) ? "FOUND OPTIMUM at " + iteration : ("EXCEEDED " + endAtIterationMax + " ITERATIONS")) +
            "\n BEST VALUE: " + bestIndividual);
    }




    void TestTrace(ref int testID, bool pass ) //[ADD MESSAGE]
    {
        if (pass) Debug.Log("<b>TEST " + (testID++) + ": <color=green>PASS</color> </b>");
        else { Debug.Log("<b>TEST " + (testID++) + ": <color=red>FAILED</color></b>"); Debug.LogException(new System.Exception("TEST EXCEPTION")); }
    }


    /*public IEnumerator Tests()
    {
        int testID = 0;
        uint testPopulationSize = 10;

        //Creación de población
        population = new Population(optimizationFunction,OptimizationType.MINIMIZE);
        //1.Inicializar poblabción
        population.InitPopulation((int)testPopulationSize,0.2f);

        #region Pruebas Selección
        //2.Selección
        List<Individual> selectedChromosomes;
        //2.a) Selección Elitista
        selectedChromosomes = population.ElitistSelection((uint)testPopulationSize / 2);
        if (selectedChromosomes.Count == 5) { TestTrace(ref testID, true); }
        else { TestTrace(ref testID, false); }

        bool error = false;
        for (int position = 0; position < selectedChromosomes.Count && !error; position++) {
            foreach (var elem in population.getAtNthBestIndividuals(position))
                if (!selectedChromosomes.Contains(elem)) { TestTrace(ref testID, false); error = true; }
        }
        if(!error) { TestTrace(ref testID, true); }
        //2.b) Selección de Ruleta
        HashSet<Individual> nonRepeatedParents;
        selectedChromosomes = population.RouletteSelection(5, out nonRepeatedParents);
        if (selectedChromosomes.Count == 5) TestTrace(ref testID, true);
        else TestTrace(ref testID, false);

        //2.c) Selección de Ruleta basada en Ranking 
        selectedChromosomes = population.RankBasedRouletteSelection(8, 5, out nonRepeatedParents);
        if (selectedChromosomes.Count == 5) TestTrace(ref testID, true);
        else TestTrace(ref testID, false);
        if (selectedChromosomes.Contains(population.getAtNthBestIndividual(9)))  TestTrace(ref testID,false);
        else TestTrace(ref testID, true);

        //2.d) SUS (Stochastic Universal Sampling)
        selectedChromosomes = population.SUSSelection(7, out nonRepeatedParents);
        if (selectedChromosomes.Count == 7) TestTrace(ref testID, true);
        else TestTrace(ref testID, false);

        //2.e) Selección por torneos
        selectedChromosomes = population.KWayTournamentSelection(3, 8, out nonRepeatedParents);
        if (selectedChromosomes.Count == 8) TestTrace(ref testID, true);
        else TestTrace(ref testID, false);

        yield return new WaitForSeconds(5f);
        Debug.ClearDeveloperConsole();

        #endregion Pruebas Selección

        Individual i  = new Individual(Random.value.ToString(), "000011000010", optimizationFunction.Width, optimizationFunction.Height);
        Debug.Log(i);




        List<Individual> newPopulation  = population.UniformCrossOver(selectedChromosomes, (uint) populationSize);

        Population.PrintSelectedChromosomes(newPopulation);




    }

    */













}
