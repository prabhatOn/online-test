using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace OnlineAssessment.Compiler.Services
{
    public class CodeExecutionService : ICodeExecutionService
    {
        public async Task<CodeExecutionResult> ExecuteCodeAsync(string code, string language, int questionId)
        {
            var result = new CodeExecutionResult();
            var sw = new Stopwatch();
            
            try
            {
                sw.Start();
                
                // For now, we'll just compile and run C# code
                // In a real implementation, you would:
                // 1. Have proper test cases for each question
                // 2. Support multiple languages
                // 3. Run code in a sandboxed environment
                // 4. Have proper memory and time limits
                
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var assemblyName = Path.GetRandomFileName();
                var references = new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
                };

                var compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    EmitResult emitResult = compilation.Emit(ms);

                    if (!emitResult.Success)
                    {
                        result.Success = false;
                        result.Error = string.Join("\n", emitResult.Diagnostics);
                        return result;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(ms);
                    var type = assembly.GetType("Solution");
                    var method = type?.GetMethod("LcaDeepestLeaves");

                    if (method == null)
                    {
                        result.Success = false;
                        result.Error = "Could not find method 'LcaDeepestLeaves' in class 'Solution'";
                        return result;
                    }

                    // Create test case input
                    var treeNode = CreateTestTreeNode();
                    
                    // Execute the method
                    var instance = Activator.CreateInstance(type);
                    var output = method.Invoke(instance, new object[] { treeNode });
                    
                    sw.Stop();
                    
                    // Verify the result
                    result.Success = true;
                    result.Output = output?.ToString() ?? "null";
                    result.Runtime = sw.Elapsed.TotalMilliseconds;
                    result.Memory = Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0); // Convert to MB
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }

        private TreeNode CreateTestTreeNode()
        {
            // Create the test tree from the example:
            // [3,5,1,6,2,0,8,null,null,7,4]
            var root = new TreeNode(3);
            root.left = new TreeNode(5);
            root.right = new TreeNode(1);
            root.left.left = new TreeNode(6);
            root.left.right = new TreeNode(2);
            root.right.left = new TreeNode(0);
            root.right.right = new TreeNode(8);
            root.left.right.left = new TreeNode(7);
            root.left.right.right = new TreeNode(4);
            return root;
        }
    }

    public class TreeNode
    {
        public int val;
        public TreeNode left;
        public TreeNode right;
        public TreeNode(int val = 0, TreeNode left = null, TreeNode right = null)
        {
            this.val = val;
            this.left = left;
            this.right = right;
        }
    }
} 