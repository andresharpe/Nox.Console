<# GooSql.psm1 #>

class GooSql {

    [string]$Name
    [Object]$Goo

    GooSql( [Object]$goo ){
        $this.Name = "Sql"
        $this.Goo = $goo
    }

    [string] ToString()
    {
        return "Goo"+$this.Name+"Module"
    }

    [void] Query( [string]$connString, [string]$sql ){
        if( -not ((Get-Module -ListAvailable -Name "SqlServer"))) {
            $this.Goo.Error("Module not found. Please run 'Install-Module -Name SqlServer' as admin")
        }
        Invoke-Sqlcmd -ConnectionString $connString -Query $sql | Format-Table -AutoSize | Out-Host
        if( -not $? ) { $this.Goo.Error( "Sql error." ) }
    }

    [bool] TestConnection([string]$connectionString) {
        try {
            $sqlConnection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
            $sqlConnection.Open()
            return $true
        } catch {
            return $false
        } finally {
            $sqlConnection.Close()
        }
    }

    [bool] WaitForConnection([string]$connectionString, [int]$seconds) {
        $ret = $false
        for($i = $seconds; $i -gt 0; $i--) {
            if($this.TestConnection($connectionString)) {
                $ret = $true
                break
            }
            $this.Goo.Sleep(1)
        }
        return $ret
    }
}
