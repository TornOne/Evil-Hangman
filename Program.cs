using System;
using System.Collections.Generic;
using System.IO;

class Program {
	static void Main() {
		Random rng = new Random();

		//Read in all the words and filter by length
		string[] allWords = File.ReadAllText("words.txt").Split('\n');
		Dictionary<int, List<string>> wordLengths = new Dictionary<int, List<string>>();
		foreach (string word in allWords) {
			if (word.Length > 1) {
				if (wordLengths.TryGetValue(word.Length, out List<string> words)) {
					words.Add(word);
				} else {
					wordLengths[word.Length] = new List<string>() { word };
				}
			}
		}
		//Discard any lengths with less than 100 words in it, find the longest and shortest allowed length, and count the number of words
		int longestLength = 0, shortestLength = 99, totalWords = 0;
		HashSet<char> letters = new HashSet<char>(); //Also keep track of all letters that exist, to limit guesses
		foreach (KeyValuePair<int, List<string>> wordLength in new Dictionary<int, List<string>>(wordLengths)) {
			if (wordLength.Value.Count < 100) {
				wordLengths.Remove(wordLength.Key);
			} else { //If we keep a length, that may be the longest or shortest length
				longestLength = Math.Max(longestLength, wordLength.Key);
				shortestLength = Math.Min(shortestLength, wordLength.Key);
				totalWords += wordLength.Value.Count;
				foreach (string word in wordLength.Value) { //Add all letters in all words
					foreach (char letter in word) {
						letters.Add(letter);
					}
				}
			}
		}
		if (totalWords == 0) {
			Console.WriteLine("There are not enough words in the given file.");
			Console.ReadKey(true);
			return;
		}

		//Prompt the user for a length
		Console.WriteLine($"Choose a word length between {shortestLength} and {longestLength}, inclusive (or write something else and we'll pick a random length for you):");
		if (int.TryParse(Console.ReadLine(), out int length) && length >= shortestLength && length <= longestLength) {
			if (!wordLengths.ContainsKey(length)) {
				Console.WriteLine("Sorry, not enough words of the given length, choosing a random length.");
				length = PickRandomLength();
			}
		} else {
			length = PickRandomLength();
		}
		int PickRandomLength() {
			int n = rng.Next(0, totalWords);
			foreach (KeyValuePair<int, List<string>> wordLength in wordLengths) {
				n -= wordLength.Value.Count;
				if (n < 0) {
					return wordLength.Key;
				}
			}
			throw new InvalidOperationException("This program location is thought to be unreachable.");
		}
		List<string> possibleWords = wordLengths[length];

		List<char> guesses = new List<char>();
		int wrongGuesses = 0;
		string answer = new string('_', length);
		//Let the game begin
		while (true) {
			//Print progress
			Console.WriteLine(answer);
			Console.WriteLine("Guesses: " + string.Join(" ", guesses));
			Console.WriteLine($"Wrong guesses: {wrongGuesses}/6");
			Console.WriteLine();

			//Let the user guess a letter
			Console.WriteLine("Guess a letter:");
			char guess = Console.ReadKey().KeyChar;
			Console.WriteLine();
			if (guesses.Contains(guess)) {
				Console.WriteLine("You already guessed " + guess);
				continue;
			} else if (!letters.Contains(guess)) {
				Console.WriteLine("Invalid letter, pick something else");
				continue;
			}
			guesses.Add(guess);

			//Whether the letter is admitted as existing or not, find the largest group of words that are possible after the guess
			Dictionary<List<int>, List<string>> solutions = new Dictionary<List<int>, List<string>>(new ListComparer<int>()); //Two words are considered equal solutions if they contain the same amount of the guessed letter at the same positions. (The word lengths and previous guessed letters are already guaranteed to match.)

			//Categorize each remaining possible word into one of the solution groups
			foreach (string word in possibleWords) {
				List<int> positions = new List<int>();
				for (int i = 0; i < word.Length; i++) {
					if (word[i] == guess) {
						positions.Add(i);
					}
				}
				if (solutions.TryGetValue(positions, out List<string> words)) {
					words.Add(word);
				} else {
					solutions[positions] = new List<string>() { word };
				}
			}

			//Find the best solution groups - the solution groups which have the most words in them - and choose one group from them at random
			List<KeyValuePair<List<int>, List<string>>> bestSolutions = new List<KeyValuePair<List<int>, List<string>>>();
			foreach (KeyValuePair<List<int>, List<string>> solution in solutions) {
				if (bestSolutions.Count == 0 || solution.Value.Count == bestSolutions[0].Value.Count) {
					bestSolutions.Add(solution);
				} else if (solution.Value.Count > bestSolutions[0].Value.Count) {
					bestSolutions.Clear();
					bestSolutions.Add(solution);
				}
			}
			KeyValuePair<List<int>, List<string>> chosenSolution = bestSolutions[rng.Next(0, bestSolutions.Count)];

			//Update possible words and give feedback to the player
			possibleWords = chosenSolution.Value;
			if (chosenSolution.Key.Count == 0) {
				Console.WriteLine("Incorrect guess!");
				if (++wrongGuesses >= 6) {
					Console.WriteLine($"Game over, the word was {possibleWords[rng.Next(0, possibleWords.Count)]}!");
					Console.ReadKey(true);
					break;
				}
			} else {
				Console.WriteLine($"Correct guess! {chosenSolution.Key.Count}x {guess}");
				char[] answerArray = answer.ToCharArray();
				foreach (int i in chosenSolution.Key) {
					answerArray[i] = guess;
					answer = new string(answerArray);
				}
				if (!answer.Contains("_")) {
					Console.WriteLine($"You guessed it! The word was \"{answer}\"!");
					Console.ReadKey(true);
					break;
				}
			}
		}
	}

	class ListComparer<T> : EqualityComparer<IList<T>> where T : IEquatable<T> {
		public override bool Equals(IList<T> x, IList<T> y) {
			if (x.Count != y.Count) {
				return false;
			}
			for (int i = 0; i < x.Count; i++) {
				if (!x[i].Equals(y[i])) {
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode(IList<T> list) {
			int hash = list.Count;
			foreach (T t in list) {
				hash *= t.GetHashCode();
			}
			return hash;
		}
	}
}
