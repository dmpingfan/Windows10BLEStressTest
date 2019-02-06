$builds = gci .\Windows10BLEStressTest\bin Windows10BLEStressTest.exe -rec

if ($builds.count -gt 1) {

    rm -r .\Windows10BLEStressTest\bin

    "Found multiple builds.  Maybe we'd run the wrong one?  We deleted them all so you can build the one you want." | write-warning

    return;
}

if ($builds.count -eq 0) {
    "No builds were found.  Build again." | write-warning
    return;
}

$build = $builds[0].fullname

Start-Process $build -ArgumentList ("-t","1", "-w", "1")
Start-Process $build -ArgumentList ("-t","10", "-w", "10")
Start-Process $build -ArgumentList ("-t","10", "-w", "10")
Start-Process $build -ArgumentList ("-t","10", "-w", "10")
