﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DES
{
    public class MainWindowViewModel
    {
        private const int
            SOURCEBLOCKSIZE = 64,
            CHARSIZE = 8,
            KEYBLOCKSIZE = 56,
            ROUNDS = 16;

        private const string KEY = "lifepain";

        private string[] roundKeys = new string[16];
        private string[,] LRs = new string[2, 17];

        //TODO: divide this into multiple functions
        private IEnumerable<string> CompleteAndDivide(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("Text for encryption cannot be empty");

            while ((text.Length * CHARSIZE) % SOURCEBLOCKSIZE != 0)
                text += "#";

            var blocksCount = text.Length * CHARSIZE / SOURCEBLOCKSIZE;

            for (int i = 0; i < blocksCount; i++)
            {
                yield return text.Substring(i * CHARSIZE, CHARSIZE)
                    .Select(x => CharToBinary(x))
                    .Aggregate("", (prev, next) => prev + next);
            }
        }

        public MainWindowViewModel()
        {
            var text = CompleteAndDivide("#Eg«Íï");
            var initialKey = MakeKey("4Wy¼ßñ");
            var key = Transpose(TransposeType.CompressedKey, initialKey, ManagerMatrix.GetCompressedKeyMatrix());

            var keyParts = new string[17, 2];
            keyParts[0, 0] = key.Substring(0, key.Length / 2);
            keyParts[0, 1] = key.Substring(key.Length / 2);

            var oneTimeShift = new[] { 1, 2, 9, 16 };

            for (int i = 1; i < 17; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    keyParts[i, j] = Shift(keyParts[i - 1, j], oneTimeShift.Contains(i) ? 1 : 2);
                }
            }

            for (int i = 0; i < 16; i++)
            {
                roundKeys[i] = Transpose(
                    TransposeType.RoundKey,
                    string.Join("", keyParts[i + 1, 0], keyParts[i + 1, 1]), // +1 coz this this array contains 17 elements
                    ManagerMatrix.GetPBoxCompressMatrix()
                );
            } // TODO: another function

            foreach (var block in text)
            {
                
            }
        }

        /// <summary>
        /// Translates a single char into its binary form
        /// </summary>
        /// <param name="char"></param>
        /// <returns></returns>
        private string CharToBinary(char @char)
        {
            var ch = Convert.ToString(@char, 2);

            while (ch.Length < CHARSIZE)
            {
                ch = "0" + ch; // add up to 8 bits if needed
            }

            return ch;
        }

        private string InitialPermutation(string text)
        {
            return Transpose(TransposeType.Message, text, ManagerMatrix.GetInitialPermutationMatrix());
        }

        private string MakeKey(string keyword)
        {
            var key = keyword
                .Select(x => CharToBinary(x))
                .Aggregate("", (prev, next) => prev + next);

            var a = ManagerMatrix.GetInitialPermutationMatrix();

            return key;
        }

        private string GenerateKey()
        {
            var random = new Random();

            int[] bits = new int[64];

            for (int i = 0; i < 64; i++)
            {
                if (i != 0 && (i + 1) % 8 == 0)
                {
                    var onesCount = bits.Skip(((i + 1) / 8 - 1) * 8).Take(7).Aggregate(0, (prev, next) => prev + next);
                    bits[i] = onesCount % 2 == 0 ? 1 : 0;
                    continue;
                }

                bits[i] = random.Next(0, 2);
            }

            return bits.Aggregate("", (prev, next) => prev + next);
        }

        /// <summary>
        /// Transposing bytes in <paramref name="source"/> according to defined <paramref name="matrix"/>
        /// </summary>
        /// <param name="type">type of transposing</param>
        /// <param name="source">text to transpose</param>
        /// <param name="matrix">matrix used to transpose</param>
        /// <returns></returns>
        private string Transpose(TransposeType type, string source, int[,] matrix)
        {
            char[] temp = null;
            int N = -1, M = -1; // rows and cols 

            switch (type)
            {
                case TransposeType.Message:
                    temp = new char[64];
                    N = M = 8;
                    break;
                case TransposeType.CompressedKey:
                    temp = new char[56];
                    N = 4; M = 14;
                    break;
                case TransposeType.RoundKey:
                    temp = new char[48];
                    N = 6; M = 8;
                    break;
                case TransposeType.ExpandedBlock:
                    temp = new char[48];
                    N = 8; M = 6;
                    break;
                case TransposeType.SBoxPermutation:
                    temp = new char[32];
                    N = 8; M = 4;
                    break;
            }

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    temp[i * M + j] = source[matrix[i, j] - 1];
                }
            }

            return temp.Aggregate("", (prev, next) => prev + next);
        }

        private void Swap(ref char a, ref char b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        private string Shift(string key, int times)
        {
            for (int i = 0; i < times; i++)
            {
                key = key.Substring(1, key.Length - 1) + key.Substring(0, 1);
            }

            return key;
        }

        /// <summary>
        /// XOR operation of bits between key and expanded block followed by SBox operations and its permutation
        /// </summary>
        /// <param name="block">Expanded block of (n-1) round</param>
        /// <param name="key">Round key</param>
        /// <returns></returns>
        private string FFunction(string block, string key)
        {
            var expression = XOROperation(key, block);
            var SBoxedExpression = SBoxOperation(expression);
            return SBoxPermutation(SBoxedExpression);
        }

        private string ProcessSBox(string text, int[,] sbox)
        {
            int row = Convert.ToInt32(text[0].ToString() + text[5].ToString(), 2);
            int col = Convert.ToInt32(text.Substring(1, 4), 2);

            var result = Convert.ToString(sbox[row, col], 2);

            while(result.Length < 4)
            {
                result = "0" + result;
            }

            return result;
        }

        private string SBoxOperation(string expression)
        {
            var result = string.Empty;

            for(int i = 0; i < 8; i++)
            {
                result += ProcessSBox(expression.Substring(i * 6, 6), ManagerMatrix.GetSBoxMatrix(i+1));
            }

            return result;
        }

        private string XOROperation(string left, string right)
        {
            var output = new char[48];

            for (int i = 0; i < 48; i++)
            {
                output[i] = left[i].Equals(right[i]) ? '0' : '1';
            }

            return string.Join("", output);
        }

        private string SBoxPermutation(string expression)
        {
            return Transpose(TransposeType.SBoxPermutation, expression, ManagerMatrix.GetSBoxPermutationMatrix());
        }

        private void EncodeBlock(string block)
        {
            var permutatedText = InitialPermutation(block);

            LRs[0, 0] = permutatedText.Substring(0, permutatedText.Length / 2);
            LRs[0, 1] = permutatedText.Substring(permutatedText.Length / 2);

            GetLeftRight();
        }

        private void GetLeftRight()
        {
            for (int i = 1; i < 17; i++)
            {
                LRs[i, 0] = LRs[i - 1, 1];

                var expandedBlock = Transpose(TransposeType.ExpandedBlock, LRs[i - 1, 1], ManagerMatrix.GetExpandedBlockMatrix());
                var expression = FFunction(expandedBlock, roundKeys[0]);

                LRs[i, 1] = XOROperation(LRs[i - 1, 0], expression);
            }
        }
    }
}
