using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClueManager : MonoBehaviour
{
    /*
- Types of clues to create for townsfolk:
* Saw person in a location (with eyes emote) or saw them on the way to a location (footsteps)
* (if location seen in also equals their workplace) saw them at work
* Comment on facial features
* Person wearing clothes
* Gossip

- Types of clues to create for ghosts:
* Visual clues (hairstyle, facial, clothing)
     */

    
    public bool IsGeneratingClues = false;

    // List of clues generated by ghosts, so that we can keep track of them. This'll allow us
    //  to make sure (some amount of) duplicate clues are given as these clues are arguably
    //  the most valuble and the player will want to correlate them
    private List<ClueObject> ghostGeneratedClues = new List<ClueObject>();
    private int iNumberOfGhostLiesGiven = 0;

    private int iNumReportingWerewolfLocation = 0;

    void Awake()
    {
        Service.Clue = this;
    }

    public void GenerateCluesForCurrentPhase()
    {
        if (!IsGeneratingClues)
        {
            IsGeneratingClues = true;
            StartCoroutine(GenerateClues(Service.PhaseSolve.CurrentPhase));
        }
    }

    IEnumerator GenerateClues(Phase currentPhase)
    {
        // Characters try to generate a clue of each type and then will give out a clue depending on random weights

        iNumReportingWerewolfLocation = 0;

        // Randomly process character clues so that we don't bias any chance or sequence based logic to the 
        //  characters at the front of the list (iNumReportingWerewolfLocation)
        List<int> vRandomProcessingOrder = Randomiser.GetRandomCharacterProcessingOrder();

        //foreach (var c in Service.Population.ActiveCharacters)
        foreach(var index in vRandomProcessingOrder)
        {
            var c = Service.Population.ActiveCharacters[index];

            // If dead don't generate any clues (unless we need to generate their first set of ghost clues
            //  Ghost clues are a one off generation that happens the first time after they died. They will
            //  then keep these clues until the player speaks to them.
            if (!c.IsAlive && c.HasGeneratedGhostClues)
            {
                continue;
            }

            bool bGeneratingGhostClues = (!c.IsAlive && !c.HasGeneratedGhostClues);
            float fLieChance =
                bGeneratingGhostClues
                ? Service.Config.GhostLieChance
                : c.IsWerewolf ? Service.Config.WerewolfLieChance : Service.Config.CharacterLieChance;

            currentPhase.CharacterCluesToGive.Add(c, new List<ClueObject>());

            // Alive characters generate a bunch of clues
            if (!bGeneratingGhostClues)
            {
                bool bShouldGenerateALie = fLieChance > 0.0f && UnityEngine.Random.Range(0.0f, 100.0f) < fLieChance;

                // 1) Generate saw in location clue
                GenerateSawInLocationClue(currentPhase, c, bShouldGenerateALie);

#if UNITY_EDITOR
                if(Service.Config.DebugYieldInGeneration)
                {
                    yield return new WaitForSeconds(0.02f);
                }
#else
                yield return new WaitForSeconds(0.02f);
#endif
                // 2) Generate saw passing by clue
                GenerateSawPassingByClue(currentPhase, c, bShouldGenerateALie);

#if UNITY_EDITOR
                if (Service.Config.DebugYieldInGeneration)
                {
                    yield return new WaitForSeconds(0.02f);
                }
#else
                yield return new WaitForSeconds(0.02f);
#endif

                // 3) Generate saw at work clue
                GenerateSawAtWorkClue(currentPhase, c, bShouldGenerateALie);

#if UNITY_EDITOR
                if (Service.Config.DebugYieldInGeneration)
                {
                    yield return new WaitForSeconds(0.02f);
                }
#else
                yield return new WaitForSeconds(0.02f);
#endif

                // 4) Generate comment on facial features clue
                GenerateCommentFacialClue(currentPhase, c, bShouldGenerateALie);

#if UNITY_EDITOR
                if (Service.Config.DebugYieldInGeneration)
                {
                    yield return new WaitForSeconds(0.02f);
                }
#else
                yield return new WaitForSeconds(0.02f);
#endif

                // 5) Generate comment on clothing clue
                GenerateCommentClothingClue(currentPhase, c, bShouldGenerateALie);

#if UNITY_EDITOR
                if (Service.Config.DebugYieldInGeneration)
                {
                    yield return new WaitForSeconds(0.02f);
                }
#else
                yield return new WaitForSeconds(0.02f);
#endif

                // 6) Generate gossip clue
                GenerateGossipClue(currentPhase, c);

#if UNITY_EDITOR
                if (Service.Config.DebugYieldInGeneration)
                {
                    yield return new WaitForSeconds(0.02f);
                }
#else
                yield return new WaitForSeconds(0.02f);
#endif
            }
            // Ghosts do not generate normal clues, only a "VisualFromGhost" clue
            else
            {
                // The most lies ghosts give to the player, the less of a chance they have to give a further lie
                //  possibly stopping all lies together (if configured to) after giving out a bunch.
                fLieChance -= Service.Config.GhostLieChanceFalloff * iNumberOfGhostLiesGiven;

                // Getting further into the game, the ghosts will potentially lie less often (no decrease on the first day)
                fLieChance -= Service.Config.GhostLieChanceFalloffPerDay * (Service.Game.CurrentDay - 1);

                bool bShouldGenerateALie = fLieChance > 0.0f && UnityEngine.Random.Range(0.0f, 100.0f) < fLieChance;

                // 1) Generate ghost visual clue
                GenerateGhostVisualClue(currentPhase, c, bShouldGenerateALie);

#if UNITY_EDITOR
                if (Service.Config.DebugYieldInGeneration)
                {
                    yield return new WaitForSeconds(0.02f);
                }
#else
                yield return new WaitForSeconds(0.02f);
#endif
            }

            // Generate emotes for all clues given
            foreach (var clue in currentPhase.CharacterCluesToGive[c])
            {
                clue.Generate();
            }


#if UNITY_EDITOR
            if (Service.Config.DebugYieldInGeneration)
            {
                yield return new WaitForSeconds(0.02f);
            }
#else
                yield return new WaitForSeconds(0.02f);
#endif
        }

        IsGeneratingClues = false;
        yield return null;
    }

    void GenerateSawInLocationClue(Phase currentPhase, Character character, bool bGenerateLie = false)
    {
        if(!currentPhase.CharacterSeenMap.ContainsKey(character)
            || currentPhase.CharacterSeenMap[character].Count == 0)
        {
            return;
        }

        if(bGenerateLie)
        {
            ClueObject lyingClue = new ClueObject(ClueObject.ClueType.SawInLocation)
            {
                GivenByCharacter = character,
                RelatesToCharacter = Service.Population.GetRandomCharacter(bIgnoreWerewolf: true, ignoreCharacters: new List<Character>() { character }),
                LocationSeenIn = Task.GetRandomLocation(),
                IsTruth = false
            };

            Debug.Log(string.Format("[{0}] {1} generating saw in location lie about [{2}] {3}",
                character.Index, character.Name, lyingClue.RelatesToCharacter.Index, lyingClue.RelatesToCharacter.Name));

            currentPhase.CharacterCluesToGive[character].Add(lyingClue);
            return;
        }

        // No seen characters, can't give this as a clue (unless a lie from above)
        if(currentPhase.CharacterSeenMap[character].Count == 0)
        {
            return;
        }

        List<Tuple<Character, int>> charactersSeen = currentPhase.CharacterSeenMap[character];

        float fAverageWeight = 100.0f / charactersSeen.Count;
        float fWerewolfWeight = fAverageWeight;

        if(iNumReportingWerewolfLocation == 0)
        {
            fWerewolfWeight *= 8;
        }
        else if (iNumReportingWerewolfLocation == 1)
        {
            fWerewolfWeight *= 4;
        }

        List<float> vWeights = new List<float>();

        foreach(var c in charactersSeen)
        {
            vWeights.Add(c.Item1.IsWerewolf ? fWerewolfWeight : fAverageWeight);
        }

        int iIndexToUse = Randomiser.GetRandomIndexFromWeights(vWeights);

        if(iIndexToUse == 0)
        {
            iNumReportingWerewolfLocation++;
        }

        Character toGenerateClueFrom = charactersSeen[iIndexToUse].Item1;
        int iLocationSeenIn = charactersSeen[iIndexToUse].Item2;

        Debug.Assert(toGenerateClueFrom != null);
        Debug.Assert(Emote.IsLocationValid(iLocationSeenIn));

        ClueObject clue = new ClueObject(ClueObject.ClueType.SawInLocation)
        {
            GivenByCharacter = character,
            RelatesToCharacter = toGenerateClueFrom,
            LocationSeenIn = iLocationSeenIn
        };

        currentPhase.CharacterCluesToGive[character].Add(clue);
    }

    void GenerateSawPassingByClue(Phase currentPhase, Character character, bool bGenerateLie = false)
    {
        if (!currentPhase.CharacterSawPassingByMap.ContainsKey(character))
        {
            return;
        }

        if (bGenerateLie)
        {
            ClueObject lyingClue = new ClueObject(ClueObject.ClueType.SawPassingBy)
            {
                GivenByCharacter = character,
                RelatesToCharacter = Service.Population.GetRandomCharacter(bIgnoreWerewolf: true, ignoreCharacters: new List<Character>() { character }),
                LocationSeenIn = Task.GetRandomLocation(),
                IsTruth = false
            };

            Debug.Log(string.Format("[{0}] {1} generating saw passing by lie about [{2}] {3}",
                character.Index, character.Name, lyingClue.RelatesToCharacter.Index, lyingClue.RelatesToCharacter.Name));

            currentPhase.CharacterCluesToGive[character].Add(lyingClue);
            return;
        }

        // No seen passing by characters, can't give this as a clue (unless a lie from above)
        if (currentPhase.CharacterSawPassingByMap[character].Count == 0)
        {
            return;
        }

        List<Tuple<Character, int>> charactersSawPassingBy = currentPhase.CharacterSawPassingByMap[character];

        float fAverageWeight = 100.0f / charactersSawPassingBy.Count;
        float fWerewolfWeight = fAverageWeight * 2;

        List<float> vWeights = new List<float>();

        foreach (var c in charactersSawPassingBy)
        {
            vWeights.Add(c.Item1.IsWerewolf ? fWerewolfWeight : fAverageWeight);
        }

        int iIndexToUse = Randomiser.GetRandomIndexFromWeights(vWeights);
        Character toGenerateClueFrom = charactersSawPassingBy[iIndexToUse].Item1;
        int iLocationSeenIn = charactersSawPassingBy[iIndexToUse].Item2;

        Debug.Assert(toGenerateClueFrom != null);
        Debug.Assert(Emote.IsLocationValid(iLocationSeenIn));

        ClueObject clue = new ClueObject(ClueObject.ClueType.SawPassingBy)
        {
            GivenByCharacter = character,
            RelatesToCharacter = toGenerateClueFrom,
            LocationSeenIn = iLocationSeenIn
        };

        currentPhase.CharacterCluesToGive[character].Add(clue);
    }

    void GenerateSawAtWorkClue(Phase currentPhase, Character character, bool bGenerateLie = false)
    {
        if (!currentPhase.CharacterSeenMap.ContainsKey(character))
        {
            return;
        }

        if (bGenerateLie)
        {
            ClueObject lyingClue = new ClueObject(ClueObject.ClueType.SawAtWork)
            {
                GivenByCharacter = character,
                RelatesToCharacter = Service.Population.GetRandomCharacter(bIgnoreWerewolf: true, ignoreCharacters: new List<Character>() { character }),
                LocationSeenIn = -1,
                IsTruth = false
            };

            Debug.Log(string.Format("[{0}] {1} generating seen at work lie about [{2}] {3}", 
                character.Index, character.Name, lyingClue.RelatesToCharacter.Index, lyingClue.RelatesToCharacter.Name));

            currentPhase.CharacterCluesToGive[character].Add(lyingClue);
            return;
        }

        // No seen characters, can't give this as a clue (unless a lie from above)
        if (currentPhase.CharacterSeenMap[character].Count == 0)
        {
            return;
        }

        List<Tuple<Character, int>> charactersSeen = new List<Tuple<Character, int>>();
        charactersSeen.Copy(currentPhase.CharacterSeenMap[character]);

        // Go through and remove characters that don't have occupations
        for (int i = charactersSeen.Count-1; i >= 0; --i)
        {
            if(charactersSeen[i].Item1.GetWorkType() == Emote.InvalidSubType)
            {
                charactersSeen.RemoveAt(i);
            }
        }

        // Didn't see any at work
        if(charactersSeen.Count == 0)
        {
            return;
        }

        int iIndexToUse = UnityEngine.Random.Range(0, charactersSeen.Count);
        Character toGenerateClueFrom = charactersSeen[iIndexToUse].Item1;

        Debug.Assert(toGenerateClueFrom != null);

        ClueObject clue = new ClueObject(ClueObject.ClueType.SawAtWork)
        {
            GivenByCharacter = character,
            RelatesToCharacter = toGenerateClueFrom,
            LocationSeenIn = -1
        };

        currentPhase.CharacterCluesToGive[character].Add(clue);
    }

    void GenerateCommentFacialClue(Phase currentPhase, Character character, bool bGenerateLie = false)
    {
        if (!currentPhase.CharacterSeenMap.ContainsKey(character))
        {
            return;
        }

        if (bGenerateLie)
        {
            ClueObject lyingClue = new ClueObject(ClueObject.ClueType.CommentFacialFeatures)
            {
                GivenByCharacter = character,
                RelatesToCharacter = Service.Population.GetRandomCharacter(bIgnoreWerewolf: true, ignoreCharacters: new List<Character>() { character }),
                LocationSeenIn = Task.GetRandomLocation(),
                IsTruth = false
            };

            Debug.Log(string.Format("[{0}] {1} generating comment facial features lie about [{2}] {3}",
                character.Index, character.Name, lyingClue.RelatesToCharacter.Index, lyingClue.RelatesToCharacter.Name));

            currentPhase.CharacterCluesToGive[character].Add(lyingClue);
            return;
        }

        // No seen characters, can't give this as a clue (unless a lie from above)
        if (currentPhase.CharacterSeenMap[character].Count == 0)
        {
            return;
        }

        List<Tuple<Character, int>> charactersSeen = currentPhase.CharacterSeenMap[character];

        float fAverageWeight = 100.0f / charactersSeen.Count;
        float fWerewolfWeight = fAverageWeight * 1.2f;

        List<float> vWeights = new List<float>();

        foreach (var c in charactersSeen)
        {
            vWeights.Add(c.Item1.IsWerewolf ? fWerewolfWeight : fAverageWeight);
        }

        int iIndexToUse = Randomiser.GetRandomIndexFromWeights(vWeights);
        Character toGenerateClueFrom = charactersSeen[iIndexToUse].Item1;
        int iLocationSeenIn = charactersSeen[iIndexToUse].Item2;

        Debug.Assert(toGenerateClueFrom != null);
        Debug.Assert(Emote.IsLocationValid(iLocationSeenIn));

        ClueObject clue = new ClueObject(ClueObject.ClueType.CommentFacialFeatures)
        {
            GivenByCharacter = character,
            RelatesToCharacter = toGenerateClueFrom,
            LocationSeenIn = iLocationSeenIn
        };

        currentPhase.CharacterCluesToGive[character].Add(clue);
    }

    void GenerateCommentClothingClue(Phase currentPhase, Character character, bool bGenerateLie = false)
    {
        if (!currentPhase.CharacterSeenMap.ContainsKey(character))
        {
            return;
        }

        if (bGenerateLie)
        {
            ClueObject lyingClue = new ClueObject(ClueObject.ClueType.CommentClothing)
            {
                GivenByCharacter = character,
                RelatesToCharacter = Service.Population.GetRandomCharacter(bIgnoreWerewolf: true, ignoreCharacters: new List<Character>() { character }),
                LocationSeenIn = Task.GetRandomLocation(),
                IsTruth = false
            };

            Debug.Log(string.Format("[{0}] {1} generating comment clothing lie about [{2}] {3}",
                character.Index, character.Name, lyingClue.RelatesToCharacter.Index, lyingClue.RelatesToCharacter.Name));

            currentPhase.CharacterCluesToGive[character].Add(lyingClue);
            return;
        }

        // No seen characters, can't give this as a clue (unless a lie from above)
        if (currentPhase.CharacterSeenMap[character].Count == 0)
        {
            return;
        }

        List<Tuple<Character, int>> charactersSeen = currentPhase.CharacterSeenMap[character];

        float fAverageWeight = 100.0f / charactersSeen.Count;
        float fWerewolfWeight = fAverageWeight * 1.2f;

        List<float> vWeights = new List<float>();

        foreach (var c in charactersSeen)
        {
            float weight = c.Item1.IsWerewolf ? fWerewolfWeight : fAverageWeight;

            if (c.Item1.CurrentClothingCondition != null)
            {
                if (c.Item1.CurrentClothingCondition.SubType == Emote.EmoteSubType.Condition_Bloody)
                {
                    weight *= 2;
                }
                else if (c.Item1.CurrentClothingCondition.SubType == Emote.EmoteSubType.Condition_Torn)
                {
                    weight *= 1.5f;
                }
            }

            vWeights.Add(weight);
        }

        int iIndexToUse = Randomiser.GetRandomIndexFromWeights(vWeights);
        Character toGenerateClueFrom = charactersSeen[iIndexToUse].Item1;
        int iLocationSeenIn = charactersSeen[iIndexToUse].Item2;

        Debug.Assert(toGenerateClueFrom != null);
        Debug.Assert(Emote.IsLocationValid(iLocationSeenIn));

        ClueObject clue = new ClueObject(ClueObject.ClueType.CommentClothing)
        {
            GivenByCharacter = character,
            RelatesToCharacter = toGenerateClueFrom,
            LocationSeenIn = iLocationSeenIn
        };

        currentPhase.CharacterCluesToGive[character].Add(clue);
    }

    void GenerateGossipClue(Phase currentPhase, Character character)
    {
        Character aboutCharacter = null;

        if (currentPhase.CharacterSeenMap.ContainsKey(character)
               && currentPhase.CharacterSeenMap[character].Count > 0)
        {
            int iRandIndex = UnityEngine.Random.Range(0, currentPhase.CharacterSeenMap[character].Count);
            aboutCharacter = currentPhase.CharacterSeenMap[character][iRandIndex].Item1;
        }
        else
        {
            aboutCharacter = Service.Population.GetRandomCharacter(ignoreCharacters: new List<Character>() { character });
        }

        int iLocCurrentlyIn = aboutCharacter.GetALocationCurrentlyIn();

        // If not in a location, just treat this like a lie, as location won't likely be used for the clue anyway
        if(iLocCurrentlyIn == -1)
        {
            iLocCurrentlyIn = Task.GetRandomLocation();
        }

        ClueObject clue = new ClueObject(ClueObject.ClueType.CommentGossip)
        {
            GivenByCharacter = character,
            RelatesToCharacter = aboutCharacter,
            LocationSeenIn = iLocCurrentlyIn
        };

        currentPhase.CharacterCluesToGive[character].Add(clue);
    }

    void GenerateGhostVisualClue(Phase currentPhase, Character character, bool bGenerateLie = false)
    {
        Character ww = Service.Population.GetWerewolf();

        if(bGenerateLie)
        {
            ClueObject lyingClue = new ClueObject(ClueObject.ClueType.VisualFromGhost)
            {
                GivenByCharacter = character,
                RelatesToCharacter = ww,
                LocationSeenIn = character.DeathLocation,
                GhostGivenClueType = Character.GetRandomDescriptorType(new List<Character.Descriptor>() { Character.Descriptor.Occupation }),
                IsTruth = false
            };

            Debug.Log(string.Format("Ghost [{0}] {1} generating visual clue lie about werewolf [{2}] {3}",
                character.Index, character.Name, lyingClue.RelatesToCharacter.Index, lyingClue.RelatesToCharacter.Name));

            currentPhase.CharacterCluesToGive[character].Add(lyingClue);
            ghostGeneratedClues.Add(lyingClue);

            iNumberOfGhostLiesGiven++;

            return;
        }

        Character.Descriptor eDescriptorTypeToGive;

        bool bGiveUniqueDescriptions = 
            Service.Config.AllowLateGameUniqueIdentifiersFromGhosts &&
            Service.Population.iNumberOfCharactersDead >= Service.Config.NumberOfDeathsToClassifyLateGame;

        Character.Descriptor eLastTrueDescriptorType = Character.InvalidDescriptor;
        List<Character.Descriptor> eAllTruthGivenTypes = new List<Character.Descriptor>();

        for(int i = ghostGeneratedClues.Count - 1; i >= 0; --i)
        {
            if(ghostGeneratedClues[i].IsTruth)
            {
                if (eLastTrueDescriptorType == Character.InvalidDescriptor)
                {
                    eLastTrueDescriptorType = ghostGeneratedClues[i].GhostGivenClueType;
                    eAllTruthGivenTypes.Add(ghostGeneratedClues[i].GhostGivenClueType);
                }
            }
        }

        // Sequence what kind of descriptors truth saying ghosts give
        switch (ghostGeneratedClues.Count - iNumberOfGhostLiesGiven)
        {
            case 0:
                {
                    // First truth ghost gives a random 
                    eDescriptorTypeToGive = Character.GetRandomDescriptorType(new List<Character.Descriptor>() { Character.Descriptor.Occupation });
                    break;
                }
            case 1:
                {
                    // Shouldn't be able to not grab the last one
                    Debug.Assert(eLastTrueDescriptorType != Character.InvalidDescriptor);

                    // Second truth gives something that isn't the last one
                    eDescriptorTypeToGive = Character.GetRandomDescriptorType(new List<Character.Descriptor>() { eLastTrueDescriptorType, Character.Descriptor.Occupation });

                    break;
                }
            case 2:
                {
                    Debug.Assert(eAllTruthGivenTypes.Count == 2);

                    List<Character.Descriptor> descriptorsToIgnore = Character.GetAllDescriptorsInAList();

                    for(int i = descriptorsToIgnore.Count - 1; i >= 0; --i)
                    {
                        // Keep in ignoring occupation
                        if(descriptorsToIgnore[i] == Character.Descriptor.Occupation)
                        {
                            continue;
                        }

                        // Otherwise if the descriptor isn't one of the two we've already given, remove it
                        if(!eAllTruthGivenTypes.Contains(descriptorsToIgnore[i]))
                        {
                            descriptorsToIgnore.RemoveAt(i);
                            break;
                        }
                    }

                    // For this third true clue, we get a duplicate of one of the previous clues
                    eDescriptorTypeToGive = Character.GetRandomDescriptorType(descriptorsToIgnore);

                    break;
                }
            default:
                {
                    // Then from the fourth true clue onwards we have the chance to generate occupation descriptors
                    // Allow occupation descriptor clues if:
                    // > The werewolf has an occupation
                    // > it's not unique to the werewolf
                    // > it is unique and we can now give unique descriptions
                    //  Population generation should ensure that occupation can be the only unique descriptor
                    bool bGenOccupation = ww.GetWorkType() != Emote.InvalidSubType
                        && (Service.Population.MatchingDescriptorMap[Character.Descriptor.Occupation].Count > 0
                        || bGiveUniqueDescriptions);

                    List<Character.Descriptor> ignoreList = new List<Character.Descriptor>();
                    if(!bGenOccupation)
                    {
                        ignoreList.Add(Character.Descriptor.Occupation);
                    }

                    eDescriptorTypeToGive = Character.GetRandomDescriptorType(ignoreList);
                    break;
                }
        }

        ClueObject clue = new ClueObject(ClueObject.ClueType.VisualFromGhost)
        {
            GivenByCharacter = character,
            RelatesToCharacter = ww,
            LocationSeenIn = character.DeathLocation,
            GhostGivenClueType = eDescriptorTypeToGive
        };

        currentPhase.CharacterCluesToGive[character].Add(clue);
        ghostGeneratedClues.Add(clue);
    }
}
