/*******************************************************************************
 * Copyright (c) 2015 itemis AG (http://www.itemis.eu) and others.
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *******************************************************************************/
package org.eclipse.xtext.example.arithmetics.tests

import com.google.inject.Inject
import org.eclipse.xtext.example.arithmetics.arithmetics.Module
import org.eclipse.xtext.junit4.InjectWith
import org.eclipse.xtext.junit4.XtextRunner
import org.eclipse.xtext.junit4.util.ParseHelper
import org.eclipse.xtext.resource.SaveOptions
import org.eclipse.xtext.serializer.ISerializer
import org.junit.Test
import org.junit.runner.RunWith

import static org.junit.Assert.*

@RunWith(XtextRunner)
@InjectWith(ArithmeticsInjectorProvider)
class ArithmeticsFormatterTest {

    @Inject extension ParseHelper<Module> parseHelper
    @Inject extension ISerializer serializer

    @Test
    def void myFormatTest() {
        '''
            module   evaluation    
                 
            def     weightedsum   (     a , b ) : 
                        2*a+5*b
               ;   
             
            weightedsum(10, 12); 
            
            
               weightedsum   (   0   ,    1   )   ;    
            
            
            ( weightedsum(1, 0))  ;
            
                15   *     44    +12 ;  
            
            
        '''.toString.assertFormattedEquals('''
            module evaluation
            def weightedsum(a, b): 2 * a + 5 * b;
            weightedsum(10, 12);
            weightedsum(0, 1);
            (weightedsum(1, 0));
            15 * 44 + 12;
        ''')
    }

    def void assertFormattedEquals(String input, String expectedFormat) {
        val module = parseHelper.parse(input)
        assertEquals(expectedFormat, module?.serialize(SaveOptions::newBuilder.format().getOptions()))
    }

}
