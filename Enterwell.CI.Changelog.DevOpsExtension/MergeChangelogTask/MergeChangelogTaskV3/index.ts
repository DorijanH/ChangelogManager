/**
 * File containing all the logic of our task.
 */
import path = require('path');
import tl = require('azure-pipelines-task-lib/task');
import fs = require('fs');
import { execFileSync, spawnSync } from 'child_process';

/**
 * Prints the contents of the changelog and changes location passed to the task.
 * 
 * @param changelogLocation Path to the directory where 'Changelog.md' is located.
 * @param changesLocation Path to the 'changes' directory.
 */
function printContents(changelogLocation: string, changesLocation: string) {

  // Print the contents of the directory that should contain 'Changelog.md'.
  let changelogName: string;

  try {
    var folderFiles = fs.readdirSync(changelogLocation, {encoding: 'utf-8'});

    changelogName = 'changelog.md';

    console.log('=======FOLDER CONTENTS=======');
    folderFiles.forEach((file) => {

      if (file.toLowerCase() === changelogName) {
        changelogName = file;
      }

      console.log(file);
  })
  } catch(err) {
    throw new Error('Error occurred while reading changelog directory.\n' + err);
  }

  // Print the contents of the 'changes' directory.
  try {
    let changesFiles = fs.readdirSync(changesLocation, {encoding: 'utf-8'});

    console.log('=======CHANGES CONTENTS=======');
    changesFiles.forEach((file) => {
      console.log(file);
    })
  } catch (err) {
    throw new Error('Error occurred while reading changes folder.\n' + err);
  }

  // Print the changelog.md contents
  let changelogPath = path.join(changelogLocation, changelogName);

  try {
    var changelogFile = fs.readFileSync(changelogPath, {encoding: 'utf-8'});

    console.log('=======CHANGELOG.MD=======');
    console.log(changelogFile);
  } catch (err){
    throw new Error('Error occurred while reading changelog file.\n' + err);
  }
}

/**
 * Entry method that runs the task.
 */
async function run() {
  try {

    // Get inputs.
    let input_changelogLocation: string = tl.getPathInput('changelogLocation', true, true) || '';
    let input_differentLocation: boolean = tl.getBoolInput('changesInDifferentLocation');
    let input_changesLocation: string;
    let input_shouldBumpVersion: boolean = tl.getBoolInput('shouldBumpVersion');
    let input_pathToProjectFile: string = tl.getInput('pathToProjectFile') || ''; 

    if (!input_differentLocation) {
      input_changesLocation = path.join(input_changelogLocation, 'changes');
    } else {
      input_changesLocation = tl.getPathInput('changesLocation', true, true) || '';
    }

    console.log('Input changelog location: ' + input_changelogLocation);
    console.log('Input different location: ' + input_differentLocation);
    console.log('Input changes location: ' + input_changesLocation);
    console.log('Input should bump version: ' + input_shouldBumpVersion);
    console.log('Input path to the project file: ' + input_pathToProjectFile);

    if (!(input_changesLocation.endsWith('changes'))) {
      throw new Error('Insert correct changes location!');
    }
    
    console.log('=============================================BEFORE EXECUTION=============================================');
    printContents(input_changelogLocation, input_changesLocation);

    try {
      let setVersionProjectFilePath = input_pathToProjectFile !== '' ? `:${input_pathToProjectFile}` : '';
      let setVersionOption = input_shouldBumpVersion ? `-sv${setVersionProjectFilePath}` : null;

      let fileToRunPath;
      let newlyBumpedVersion;

      // If on windows VM
      if (process.platform === 'win32') {
        fileToRunPath = path.join(__dirname, 'cl.exe');
        
        if (setVersionOption == null) {
          newlyBumpedVersion = execFileSync(fileToRunPath, [input_changelogLocation, input_changesLocation], { encoding: 'utf-8' });
        } else {
          newlyBumpedVersion = execFileSync(fileToRunPath, [input_changelogLocation, input_changesLocation, setVersionOption], {encoding: 'utf-8'});
        }
      } else {
        fileToRunPath = path.join(__dirname, 'cl');
        fs.chmodSync(fileToRunPath, 0o777);

        let error: string;

        if (setVersionOption == null) {
          const result = spawnSync(fileToRunPath, [input_changelogLocation, input_changesLocation], { encoding: 'utf-8' });

          newlyBumpedVersion = result.stdout;
          error = result.stderr;
        } else {
          const result = spawnSync(fileToRunPath, [input_changelogLocation, input_changesLocation, setVersionOption], { encoding: 'utf-8' });

          newlyBumpedVersion = result.stdout;
          error = result.stderr;
        }

        if (error) {
          throw new Error(error);
        }
      }

      console.log('=============================================AFTER EXECUTION=============================================');  

      newlyBumpedVersion = newlyBumpedVersion.trim();
      console.log('Newly bumped version got from the executable:', newlyBumpedVersion);

      if (!(/\d+.\d+.\d+/.test(newlyBumpedVersion))) {
        throw new Error('Executable output is not in the correct format.');
      }

      // Set variable with a given name, given value, that is not a secret, but an output variable
      tl.setVariable('bumpedSemanticVersion', newlyBumpedVersion, false, true);
    } catch (err) {
      throw new Error('Error occurred while running executable.\n' + err);
    }
    
    printContents(input_changelogLocation, input_changesLocation);

  } catch (err: any) {
    tl.setResult(tl.TaskResult.Failed, err.message);
  }
}

run();