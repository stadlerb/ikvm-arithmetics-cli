package org.eclipse.xtext.example.arithmetics.formatting2

import com.google.inject.Inject
import org.eclipse.xtext.example.arithmetics.arithmetics.Definition
import org.eclipse.xtext.example.arithmetics.arithmetics.Div
import org.eclipse.xtext.example.arithmetics.arithmetics.Evaluation
import org.eclipse.xtext.example.arithmetics.arithmetics.Expression
import org.eclipse.xtext.example.arithmetics.arithmetics.FunctionCall
import org.eclipse.xtext.example.arithmetics.arithmetics.Import
import org.eclipse.xtext.example.arithmetics.arithmetics.Minus
import org.eclipse.xtext.example.arithmetics.arithmetics.Module
import org.eclipse.xtext.example.arithmetics.arithmetics.Multi
import org.eclipse.xtext.example.arithmetics.arithmetics.Plus
import org.eclipse.xtext.example.arithmetics.arithmetics.Statement
import org.eclipse.xtext.example.arithmetics.services.ArithmeticsGrammarAccess
import org.eclipse.xtext.formatting2.AbstractFormatter2
import org.eclipse.xtext.formatting2.IFormattableDocument

class ArithmeticsFormatter extends AbstractFormatter2 {
    @Inject extension ArithmeticsGrammarAccess

    def dispatch void format(Module module, extension IFormattableDocument document) {
        document.prepend(module, [noSpace])
        module.allRegionsFor.keyword('module').prepend[noSpace].append[oneSpace]
        module.allRegionsFor.assignment(moduleAccess.nameAssignment_1_1).prepend[oneSpace].append[noSpace; newLine]
        module.allRegionsFor.keyword(moduleAccess.semicolonKeyword_3_1_0).prepend[noSpace].append[newLine; noSpace]
        module.regionFor.keyword(moduleAccess.semicolonKeyword_3_2).prepend[noSpace].append[newLine; noSpace]
        for (Import imports : module.getImports()) {
            imports.format;
        }
        for (Statement statements : module.getStatements()) {
            statements.format;
        }
    }

    def dispatch void format(Import ^import, extension IFormattableDocument document) {
        import.prepend[noSpace]
        import.regionFor.keyword("import").append[oneSpace]
        import.module.prepend[oneSpace].append[noSpace; newLine]
    }

    def dispatch void format(Definition definition, extension IFormattableDocument document) {
        definition.regionFor.keyword("def").prepend[newLine; noSpace].append[oneSpace]
        definition.allRegionsFor.keyword("(").prepend[noSpace].append[noSpace]
        definition.allRegionsFor.keyword(",").prepend[noSpace].append[oneSpace]
        definition.allRegionsFor.keyword(")").prepend[noSpace]
        definition.regionFor.keyword(":").prepend[noSpace].append[oneSpace; highPriority]

        definition.expr.format;
    }

    def dispatch void format(Evaluation evaluation, extension IFormattableDocument document) {
        evaluation.prepend[newLine; noSpace]
        evaluation.expression.format
    }

    def dispatch void format(Plus expression, extension IFormattableDocument document) {
        expression.formatParentheses(document)
        expression.regionFor.keyword("+").prepend[oneSpace].append[oneSpace]
        expression.left.format
        expression.right.format
    }

    def dispatch void format(Minus expression, extension IFormattableDocument document) {
        expression.formatParentheses(document)
        expression.regionFor.keyword("-").prepend[oneSpace].append[oneSpace]
        expression.left.format
        expression.right.format
    }

    def dispatch void format(Multi expression, extension IFormattableDocument document) {
        expression.formatParentheses(document)
        expression.regionFor.keyword("*").prepend[oneSpace].append[oneSpace]
        expression.left.format
        expression.right.format
    }

    def dispatch void format(Div expression, extension IFormattableDocument document) {
        expression.formatParentheses(document)
        expression.regionFor.keyword("/").prepend[oneSpace].append[oneSpace]
        expression.left.format
        expression.right.format
    }

    def dispatch void format(FunctionCall expression, extension IFormattableDocument document) {
        expression.regionFor.crossRef(primaryExpressionAccess.funcAbstractDefinitionCrossReference_2_1_0)
        expression.formatParentheses(document)
        val leftFunctionPar = primaryExpressionAccess.leftParenthesisKeyword_2_2_0
        expression.allRegionsFor.keyword(leftFunctionPar).prepend[noSpace].append[noSpace]
        expression.allRegionsFor.keyword(",").prepend[noSpace].append[oneSpace]
        val rightFunctionPar = primaryExpressionAccess.rightParenthesisKeyword_2_2_3
        expression.allRegionsFor.keyword(rightFunctionPar).prepend[noSpace].append[noSpace]

        expression.args.forEach[format]
    }

    def void formatParentheses(Expression expression, extension IFormattableDocument document) {
        val leftPar = primaryExpressionAccess.leftParenthesisKeyword_0_0
        expression.allRegionsFor.keyword(leftPar).prepend[noSpace].append[noSpace]
        val rightPar = primaryExpressionAccess.rightParenthesisKeyword_0_2
        expression.allRegionsFor.keyword(rightPar).prepend[noSpace].append[noSpace]
    }
}
