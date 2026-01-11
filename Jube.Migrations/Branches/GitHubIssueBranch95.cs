/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Migrations.Branches
{
    using FluentMigrator;

    [Migration(20260102110500)]
    public class GitHubIssueBranch95 : Migration
    {
        public override void Up()
        {
            Alter.Table("EntityAnalysisInlineScript")
                .AddColumn("LanguageId")
                .AsByte().Nullable();

            Delete.Column("ClassName").FromTable("EntityAnalysisInlineScript");
            Delete.Column("MethodName").FromTable("EntityAnalysisInlineScript");

            const string code = """
                                Imports System
                                Imports System.Net.Http
                                Imports System.Threading
                                Imports System.Threading.Tasks
                                Imports Newtonsoft.Json.Linq
                                Imports Jube.Engine.Attributes
                                Imports Jube.Engine.EntityAnalysisModelInvoke.Context
                                Imports Jube.Engine.Interfaces

                                Public Class IssueOTP
                                Implements IInlineScript
                                	Public Property OTP As String

                                	Public Async Function ExecuteAsync(context As Context) As Task Implements IInlineScript.ExecuteAsync
                                        OTP = RandomDigits(6)
                                    End Function
                                	
                                	Private Function RandomDigits(ByVal length As Integer) As String
                                	    Dim random = New Random()
                                	    Dim s As String = String.Empty

                                	    For i As Integer = 0 To length - 1
                                		s = String.Concat(s, random.[Next](10).ToString())
                                	    Next

                                	    Return s
                                	End Function	
                                End Class
                                """;

            Update.Table("EntityAnalysisInlineScript").Set(new
            {
                LanguageId = 1
            }).AllRows();

            Update.Table("EntityAnalysisInlineScript").Set(new
            {
                Code = code
            }).Where(new
            {
                Id = 1
            });
        }

        public override void Down()
        {
        }
    }
}
