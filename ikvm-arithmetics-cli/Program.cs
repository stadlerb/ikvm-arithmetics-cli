using CommandLine.Text;
using CommandLine;
using System.Collections;
using System.Collections.Generic;
using System;

using ikvm.lang;
using java.math;
using java.util;

using com.google.inject.name;
using com.google.inject;
using org.eclipse.emf.common.util;
using org.eclipse.emf.ecore.resource;
using org.eclipse.xtext.diagnostics;
using org.eclipse.xtext.resource;
using org.eclipse.xtext.serializer;
using org.eclipse.xtext.util;
using org.eclipse.xtext.validation;
using org.eclipse.xtext;
using org.eclipse.xtext.example.arithmetics.arithmetics;
using org.eclipse.xtext.example.arithmetics.interpreter;
using org.eclipse.xtext.example.arithmetics;
using org.eclipse.xtext.xbase.lib;

namespace ArithmeticsCLI {
    public class CalculatorCLI {
        static void Main(string[] args) {
            // 1. Parse argument array
            var config = new CalculatorConfig();
            if (CommandLine.Parser.Default.ParseArguments(args, config)) {
                try {
                    // 2. Do standalone setup
                    var injector = new ArithmeticsStandaloneSetup().createInjectorAndDoEMFRegistration();

                    // 3. Instantiate and execute calculator CLI
                    var cli = injector.getInstance(typeof(CalcExample)) as CalcExample;

                    // 4. Register imported files (for -e)
                    var contextFilenames = config.imports;

                    foreach (var contextFilename in contextFilenames) {
                        cli.addContextFile(contextFilename);

                    }

                    // 5. Parse the input file or expression
                    var expressionText = config.expression;

                    var filename = config.filename;

                    if (expressionText != null) {
                        cli.setInputExpression(expressionText);
                    } else if (filename != null) {
                        cli.setInputFile(filename);
                    } else {
                        Console.Error.Write(config.GetUsage());
                    }

                    // 6. Perform the calculation
                    cli.calculate();
                } catch (java.lang.Exception e) {
                    e.printStackTrace();
                }
            }
            Console.ReadKey();
        }
    }

    public class CalculatorConfig {
        [Option('f', "file", HelpText = "File to be interpreted", MutuallyExclusiveSet = "input")]
        public String filename { get; set; }
        [Option('e', "expression", HelpText = "Expression to be interpreted")]
        public String expression { get; set; }
        [OptionList('i', "import", Separator = ':', HelpText = "List of files to be imported, separated by colons (':')")]
        public IList<String> imports { get; set; } = new List<String>();

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage() {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    public class CalcExample : java.lang.Object {
        [Inject]
        private XtextResourceSet resourceSet;

        [Inject]
        private IResourceValidator validator;

        [Inject]
        private Interpreter interpreter;

        [Inject]
        private ISerializer serializer;

        [Inject Named(Constants.FILE_EXTENSIONS)]
        private String fileExtension;

        private Resource inputResource;
        private Boolean isInputExpression;

        public CalcExample() { }

        public void calculate() {
            if (inputResource == null) {
                throw new InvalidOperationException("No input expression or file defined");
            }

            var rootModule = inputResource.getContents().head<org.eclipse.xtext.example.arithmetics.arithmetics.Module>();

            // 1. Add all context modules as imports in order to avoid having to write
            //    import statements for every context module in the input expression
            if (isInputExpression) {
                importContextResources(rootModule);
            }

            // 2. Validate all resources in the resource set and print issues to console
            var issues = validateResources(resourceSet.getResources().wrap<Resource>());
            var hasErrors = false;
            foreach (var issue in issues) {
                Console.Error.WriteLine(issue.ToString());
                if (issue.getSeverity() == Severity.ERROR) {
                    hasErrors = true;
                }
            }

            if (!hasErrors) {
                doCalculate(rootModule);
            }
        }

        public void addContextFile(String filename) {
            var uri = URI.createFileURI(filename);
            if (!uri.fileExtension().Equals(fileExtension)) {
                Console.Error.WriteLine($"Please pass only *.{fileExtension} files as library arguments");
            }

            var resource = resourceSet.getResource(uri, true);
            resourceSet.getResources().add(resource);
        }


        public void setInputExpression(String text) {
            var inputUri = URI.createURI("<input>." + fileExtension);
            var resource = resourceSet.createResource(inputUri) as XtextResource;
            resource.reparse(text);
            inputResource = resource;
            isInputExpression = true;
        }



        public void setInputFile(String filename) {
            var inputUri = URI.createURI(filename);
            if (!inputUri.fileExtension().Equals(fileExtension)) {
                throw new ArgumentException($"Please pass only *.{fileExtension} files as library arguments");
            }

            inputResource = resourceSet.getResource(inputUri, true);
            resourceSet.getResources().add(inputResource);
            isInputExpression = false;
        }

        private void importContextResources(org.eclipse.xtext.example.arithmetics.arithmetics.Module rootModule) {
            var imports = rootModule.getImports();
            foreach (var resource in rootModule.eResource().getResourceSet().getResources().wrap<Resource>()) {
                if (!resource.Equals(inputResource)) {
                    var contextRoot = resource.getContents().head<org.eclipse.xtext.example.arithmetics.arithmetics.Module>();
                    var context = contextRoot as org.eclipse.xtext.example.arithmetics.arithmetics.Module;
                    var import = ArithmeticsFactory.eINSTANCE.createImport();
                    import.setModule(context);
                    imports.add(import);
                }
            }
        }

        protected IEnumerable<Issue> validateResources(IEnumerable<Resource> resources) {
            foreach (var resource in resources) {
                var issues = validator.validate(resource, CheckMode.NORMAL_AND_FAST, CancelIndicator.NullImpl).wrap<Issue>();
                foreach (var issue in issues) {
                    yield return issue;
                }
            }
        }

        protected void doCalculate(org.eclipse.xtext.example.arithmetics.arithmetics.Module root) {
            var serializerOptions = SaveOptions.newBuilder().format().getOptions();
            foreach (var evaluation in root.getStatements().wrap()) {
                if (evaluation is Evaluation) {
                    var expression = (evaluation as Evaluation).getExpression();
                    var result = interpreter.evaluate(expression);
                    Console.WriteLine($"- {serializer.serialize(expression, serializerOptions)}: {result}");
                }
            }
        }
    }

    public class Interpreter {
        public BigDecimal evaluate(Expression obj) {
            return evaluate(obj, new Dictionary<string, BigDecimal>());

        }

        public BigDecimal evaluate(Expression obj, Dictionary<String, BigDecimal> values) {
            return internalEvaluate(obj as dynamic, values);
        }

        protected BigDecimal internalEvaluate(NumberLiteral e, Dictionary<String, BigDecimal> values) {
            return e.getValue();
        }

        /** 
         * @param values the currently known values by name 
         */
        protected BigDecimal internalEvaluate(FunctionCall e, Dictionary<String, BigDecimal> values) {
            var func = e.getFunc();
            if (func is DeclaredParameter) {
                return values[e.getFunc().getName()];
            } else if (func is Definition) {
                var d = func as Definition;
                var parameters = new Dictionary<String, BigDecimal>();


                for (int i = 0; i < e.getArgs().size(); i++) {
                    var declaredParameter = d.getArgs().get(i) as DeclaredParameter;

                    var result = evaluate(e.getArgs().get(i) as Expression, values);
                    String name = declaredParameter.getName();
                    parameters[name] = result;

                }
                return evaluate(d.getExpr(), new Dictionary<String, BigDecimal>(parameters));
            } else {
                return null;
            }
        }


        protected BigDecimal internalEvaluate(Plus plus, Dictionary<String, BigDecimal> values) {
            return evaluate(plus.getLeft(), values).add(evaluate(plus.getRight(), values));

        }

        protected BigDecimal internalEvaluate(Minus minus, Dictionary<String, BigDecimal> values) {
            return evaluate(minus.getLeft(), values).subtract(evaluate(minus.getRight(), values));

        }

        protected BigDecimal internalEvaluate(Div div, Dictionary<String, BigDecimal> values) {
            return evaluate(div.getLeft(), values).divide(evaluate(div.getRight(), values), 20, RoundingMode.HALF_UP);

        }

        protected BigDecimal internalEvaluate(Multi multi, Dictionary<String, BigDecimal> values) {
            return evaluate(multi.getLeft(), values).multiply(evaluate(multi.getRight(), values));

        }
    }

    public class IterableEnumerable<T> : IEnumerable<T> where T : class {
        private java.lang.Iterable iterable;

        public IterableEnumerable(java.lang.Iterable iterable) {
            this.iterable = iterable;
        }

        public IEnumerator<T> GetEnumerator() {
            return new IterableEnumerator<T>(iterable);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new IterableEnumerator(iterable);
        }
    }


    public class IterableEnumerator<T> : IEnumerator<T> where T : class {
        private Boolean defined;
        private readonly java.lang.Iterable src;
        private Iterator iter;
        private T Current;

        T IEnumerator<T>.Current {
            get {
                if (defined) {
                    return Current;
                } else {
                    throw new InvalidOperationException();
                }
            }
        }

        object IEnumerator.Current { get; }

        public IterableEnumerator(java.lang.Iterable src) {
            this.src = src;
            this.iter = src.iterator();
        }


        void IDisposable.Dispose() {
            this.iter = null;
            defined = false;
        }

        bool IEnumerator.MoveNext() {
            if (iter.hasNext()) {
                var next = iter.next();
                if (next is T) {
                    Current = next as T;
                    defined = true;
                    return true;
                } else {
                    throw new InvalidCastException();
                }
            }
            Current = null;
            defined = false;
            return false;
        }

        void IEnumerator.Reset() {
            iter = src.iterator();
            defined = false;
        }
    }

    public static class IterableExtension {
        public static IterableEnumerable<Object> wrap(this java.lang.Iterable iterable) {
            return new IterableEnumerable<Object>(iterable);
        }

        public static IterableEnumerable<T> wrap<T>(this java.lang.Iterable iterable) where T : class {
            return new IterableEnumerable<T>(iterable);
        }

        public static T head<T>(this java.lang.Iterable iterable) where T : class {
            var first = IterableExtensions.head(iterable);
            if (first is T) {
                return first as T;
            } else {
                throw new InvalidCastException();
            }
        }
    }
}
