<?php

	// template Variable must have id, author & version fields 
	function UpdateTemplate($templateFile, $outputFile, $templateVariables)
	{
		$content = file_get_contents($templateFile);
		
		// upgrade template variables
		foreach($templateVariables as $key => $value)
		{
			$content = str_replace('{'. $key . '}', $value, $content);
		}
		
		file_put_contents($outputFile, $content);
	}

	global $argc, $argv;
	
	
	
	$names = array(
		'Core' => 'Core',
		'Forms' => 'Forms',
		'Android' => 'Android',
		'AndroidSupport' => 'AndroidSupport',
		'Tablet' => 'Tablet',
	);
	$directories = array(
		'Core' => 'Storm.Mvvm',
		'Forms' => 'Storm.Mvvm.Forms',
		'Android' => 'Storm.Mvvm.Android',
		'AndroidSupport' => 'Storm.Mvvm.Android.Support',
		'Tablet' => 'Storm.Mvvm.Tablet',
	);
	
	if($argc < 2)
	{
		echo "Usage : releaseversion.sh [Core|Forms|Android|AndroidSupport|Tablet]+ <version>";
	}

	$releases = array();
	$versionNumber = $argv[$argc - 1];
	for($i = 1 ; $i < $argc-1 ; ++$i)
	{
		$release = $argv[$i];
		
		if(isset($names[$release]))
		{
			$releases[] = $argv[$i];
		}
	}
	
	if(count($releases) == 0)
	{
		echo "Nothing to release...";
		die;
	}
	
	echo "You gonna release " . implode(' & ', $releases) . " in version " . $versionNumber . PHP_EOL;
	echo "Have you built all your projects in release mode ?" . PHP_EOL;
	echo "Are you sure ? (y/n) ";
	
	$confirmChar = trim(readline());
	if($confirmChar != "y")
	{
		die;
	}
	
	echo "Ok let's go !" . PHP_EOL;
	
	echo "# Creating release branch from develop" . PHP_EOL;
	
	$branchName = 'release/' . implode('', $releases) . '_v' . $versionNumber;
	system('git checkout -b ' . $branchName . ' develop');
	
	foreach($releases as $releaseName)
	{
		$directory = $directories[$releaseName];
		echo "# Updating nuspec file for " . $directory . PHP_EOL;
		
		$path = 'nuget_build/' . $directory;
		$nuspecFile = $path . '/' . $directory . '.nuspec';
		$templateFile = $path . '.nuspec.template';
		
		UpdateTemplate($templateFile, $nuspecFile, array('id' => $directory, 'author' => 'Julien Mialon', 'version' => $versionNumber));
		
		chdir($path);
		
		echo "Generating nuget package for " . $directory . " version " . $versionNumber;
		system("nuget pack -Verbosity quiet -NonInteractive");
		echo "Uploading nuget package " . $directory . " version " . $versionNumber . " to nuget server";
		system("nuget push -Verbosity quiet -NonInteractive *.nupkg");
		system("mv *.nupkg ../../nuget_packages");
		
		chdir('../..');
	}
	
	echo "Add new packages to git" . PHP_EOL;
	system("git add -f nuget_packages/*.nupkg");
	$commitMessage = "Build file for release " . implode(' & ', $releases) . " in version " . $versionNumber;
	$commitMessage = str_replace('"', '\\"', $commitMessage);
	
	echo "Commit packages to git" . PHP_EOL;
	foreach($releases as $release)
	{
		system('git tag -a ' . $release . '_v' . $versionNumber);
	}
	system('git commit -m "' . $commitMessage . '"');
	
	echo "Merging to branch master and develop" . PHP_EOL;;
	system('git checkout master && git merge --no-ff ' . $branchName . ' && git push');
	system('git checkout develop && git merge --no-ff ' . $branchName . ' && git push');
	system('git branch -d ' . $branchName);
	
	echo "Finished Release ! :)" . PHP_EOL;
	
	