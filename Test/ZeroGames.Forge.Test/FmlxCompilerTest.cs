// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;
using ZeroGames.Forge.Runtime;

namespace ZeroGames.Forge.Test;

/// <summary>
/// FMLX编译器测试
/// </summary>
public static class FmlxCompilerTest
{
    /// <summary>
    /// 测试Map Key属性语法糖的消除
    /// </summary>
    public static void TestMapKeyAsAttribute()
    {
        Console.WriteLine("=== FMLX Map Key属性语法糖测试 ===");

        // 测试用例1: 简单的Key-Value对
        TestSimpleKeyValue();

        // 测试用例2: 嵌套的Map结构
        TestNestedMap();

        // 测试用例3: 复杂的内容结构
        TestComplexContent();

        // 测试用例4: 非Map元素（不应该被转换）
        TestNonMapElement();

        // 测试用例5: 实际FMLX文件
        TestActualFmlxFile();
    }

    private static void TestSimpleKeyValue()
    {
        Console.WriteLine("\n--- 测试1: 简单Key-Value对 ---");

        var fmlxInput = @"
<MainRegistry>
    <CharacterRepository>
        <Character>
            <Id>101</Id>
            <AttributeMap>
                <Element Key=""100"">abc</Element>
                <Element Key=""200"">zzz</Element>
            </AttributeMap>
        </Character>
    </CharacterRepository>
</MainRegistry>";

        var expectedOutput = @"
<MainRegistry>
    <CharacterRepository>
        <Character>
            <Id>101</Id>
            <AttributeMap>
                <Element>
                    <Key>100</Key>
                    <Value>abc</Value>
                </Element>
                <Element>
                    <Key>200</Key>
                    <Value>zzz</Value>
                </Element>
            </AttributeMap>
        </Character>
    </CharacterRepository>
</MainRegistry>";

        TestCompilation("简单Key-Value对", fmlxInput, expectedOutput);
    }

    private static void TestNestedMap()
    {
        Console.WriteLine("\n--- 测试2: 嵌套Map结构 ---");

        var fmlxInput = @"
<MainRegistry>
    <CharacterRepository>
        <Character>
            <Id>101</Id>
            <NestedMap>
                <Element Key=""level1"">
                    <InnerMap>
                        <Element Key=""level2"">nested_value</Element>
                    </InnerMap>
                </Element>
            </NestedMap>
        </Character>
    </CharacterRepository>
</MainRegistry>";

        var expectedOutput = @"
<MainRegistry>
    <CharacterRepository>
        <Character>
            <Id>101</Id>
            <NestedMap>
                <Element>
                    <Key>level1</Key>
                    <Value>
                        <InnerMap>
                            <Element>
                                <Key>level2</Key>
                                <Value>nested_value</Value>
                            </Element>
                        </InnerMap>
                    </Value>
                </Element>
            </NestedMap>
        </Character>
    </CharacterRepository>
</MainRegistry>";

        TestCompilation("嵌套Map结构", fmlxInput, expectedOutput);
    }

    private static void TestComplexContent()
    {
        Console.WriteLine("\n--- 测试3: 复杂内容结构 ---");

        var fmlxInput = @"
<MainRegistry>
    <CharacterRepository>
        <Character>
            <Id>101</Id>
            <ComplexMap>
                <Element Key=""weapon"">
                    <Weapon>
                        <Id>1001</Id>
                        <Damage>50</Damage>
                    </Weapon>
                </Element>
                <Element Key=""armor"">
                    <Armor>
                        <Id>2001</Id>
                        <Defence>30</Defence>
                    </Armor>
                </Element>
            </ComplexMap>
        </Character>
    </CharacterRepository>
</MainRegistry>";

        var expectedOutput = @"
<MainRegistry>
    <CharacterRepository>
        <Character>
            <Id>101</Id>
            <ComplexMap>
                <Element>
                    <Key>weapon</Key>
                    <Value>
                        <Weapon>
                            <Id>1001</Id>
                            <Damage>50</Damage>
                        </Weapon>
                    </Value>
                </Element>
                <Element>
                    <Key>armor</Key>
                    <Value>
                        <Armor>
                            <Id>2001</Id>
                            <Defence>30</Defence>
                        </Armor>
                    </Value>
                </Element>
            </ComplexMap>
        </Character>
    </CharacterRepository>
</MainRegistry>";

        TestCompilation("复杂内容结构", fmlxInput, expectedOutput);
    }

    private static void TestNonMapElement()
    {
        Console.WriteLine("\n--- 测试4: 非Map元素（不应转换） ---");

        var fmlxInput = @"
<MainRegistry>
    <CharacterRepository>
        <Character>
            <Id>101</Id>
            <Name Key=""should_not_convert"">Hero</Name>
            <SingleElement Key=""also_should_not_convert"">value</SingleElement>
        </Character>
    </CharacterRepository>
</MainRegistry>";

        // 期望输出应该与输入相同，因为这些不是Map的Element
        var expectedOutput = fmlxInput;

        TestCompilation("非Map元素", fmlxInput, expectedOutput);
    }

    private static void TestActualFmlxFile()
    {
        Console.WriteLine("\n--- 测试5: 实际FMLX文件 ---");

        try
        {
            // 加载实际的FMLX文件
            var fmlxDoc = XDocument.Load("test_fmlx.xml");
            
            Console.WriteLine("原始FMLX文件内容:");
            Console.WriteLine(fmlxDoc.ToString(SaveOptions.None));
            
            // 编译FMLX
            var compiler = new FmlxCompiler();
            var resultDoc = compiler.Compile(fmlxDoc);
            
            Console.WriteLine("\n编译后的FML内容:");
            Console.WriteLine(resultDoc.ToString(SaveOptions.None));
            
            // 保存编译结果到文件
            resultDoc.Save("test_fml_output.xml");
            Console.WriteLine("\n✓ 编译结果已保存到 test_fml_output.xml");
            
            // 验证转换结果
            ValidateCompilationResult(fmlxDoc, resultDoc);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试异常: {ex.Message}");
        }
    }

    private static void ValidateCompilationResult(XDocument original, XDocument compiled)
    {
        Console.WriteLine("\n验证转换结果:");
        
        // 检查所有Map Element是否都被正确转换
        var originalElements = original.Descendants("Element").Where(e => e.Attribute("Key") != null).ToList();
        var compiledElements = compiled.Descendants("Element").ToList();
        
        Console.WriteLine($"原始文件中有 {originalElements.Count} 个带Key属性的Element");
        Console.WriteLine($"编译后文件中有 {compiledElements.Count} 个Element");
        
        // 检查是否所有Key属性都被转换为Key子元素
        var keyElements = compiled.Descendants("Key").ToList();
        var valueElements = compiled.Descendants("Value").ToList();
        
        Console.WriteLine($"编译后文件中有 {keyElements.Count} 个Key子元素");
        Console.WriteLine($"编译后文件中有 {valueElements.Count} 个Value子元素");
        
        if (originalElements.Count == keyElements.Count && originalElements.Count == valueElements.Count)
        {
            Console.WriteLine("✓ 所有Key属性都被正确转换为Key/Value结构");
        }
        else
        {
            Console.WriteLine("✗ Key属性转换不完整");
        }
        
        // 检查非Map元素是否保持不变
        var nonMapElements = original.Descendants().Where(e => 
            e.Name.LocalName != "Element" && e.Attribute("Key") != null).ToList();
        
        if (nonMapElements.Count > 0)
        {
            Console.WriteLine($"发现 {nonMapElements.Count} 个非Map元素带有Key属性，这些应该保持不变");
        }
    }

    private static void TestCompilation(string testName, string fmlxInput, string expectedOutput)
    {
        Console.WriteLine($"测试: {testName}");
        
        try
        {
            // 解析输入
            var inputDoc = XDocument.Parse(fmlxInput);
            
            // 编译FMLX
            var compiler = new FmlxCompiler();
            var resultDoc = compiler.Compile(inputDoc);
            
            // 解析期望输出
            var expectedDoc = XDocument.Parse(expectedOutput);
            
            // 比较结果
            var inputString = NormalizeXml(inputDoc);
            var resultString = NormalizeXml(resultDoc);
            var expectedString = NormalizeXml(expectedDoc);
            
            Console.WriteLine("输入FMLX:");
            Console.WriteLine(inputString);
            
            Console.WriteLine("\n编译结果:");
            Console.WriteLine(resultString);
            
            Console.WriteLine("\n期望输出:");
            Console.WriteLine(expectedString);
            
            if (resultString == expectedString)
            {
                Console.WriteLine("✓ 测试通过");
            }
            else
            {
                Console.WriteLine("✗ 测试失败");
                Console.WriteLine("\n差异:");
                Console.WriteLine("结果与期望不匹配");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// 标准化XML字符串以便比较
    /// </summary>
    /// <param name="doc">XML文档</param>
    /// <returns>标准化的XML字符串</returns>
    private static string NormalizeXml(XDocument doc)
    {
        return doc.ToString(SaveOptions.DisableFormatting);
    }
} 