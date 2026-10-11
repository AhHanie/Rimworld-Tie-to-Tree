#!/usr/bin/env node

// Deploys the files RimWorld needs; build/source files are deliberately omitted.
const fs = require("node:fs/promises");
const path = require("node:path");

const projectRoot = __dirname;
const defaultDestination =
  "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\Mods\\Tie to Tree";
const destination = path.resolve(process.argv[2] || defaultDestination);

const foldersToCopy = ["About", "Languages", "Textures"];
const filesToCopy = ["LoadFolders.xml"];

async function exists(target) {
  try {
    await fs.access(target);
    return true;
  } catch {
    return false;
  }
}

async function copyDirectory(source, target) {
  await fs.mkdir(target, { recursive: true });

  for (const entry of await fs.readdir(source, { withFileTypes: true })) {
    const sourcePath = path.join(source, entry.name);
    const targetPath = path.join(target, entry.name);

    if (entry.isDirectory()) {
      await copyDirectory(sourcePath, targetPath);
    } else if (entry.isFile()) {
      await fs.copyFile(sourcePath, targetPath);
    }
  }
}

async function deploy() {
  if (destination === path.parse(destination).root) {
    throw new Error("Refusing to clear a filesystem root.");
  }

  const dll = path.join(projectRoot, "1.6", "Assemblies", "Tie to Tree.dll");
  if (!(await exists(dll))) {
    throw new Error(`Missing ${dll}; build the project first.`);
  }

  // rm removes the old deployment (if any), so stale assets cannot remain.
  await fs.rm(destination, { recursive: true, force: true });
  await fs.mkdir(destination, { recursive: true });

  for (const folder of foldersToCopy) {
    const source = path.join(projectRoot, folder);
    if (await exists(source)) {
      await copyDirectory(source, path.join(destination, folder));
    }
  }

  // Copy 1.6, apart from its C# source tree.
  const versionSource = path.join(projectRoot, "1.6");
  const versionDestination = path.join(destination, "1.6");
  await fs.mkdir(versionDestination, { recursive: true });
  for (const entry of await fs.readdir(versionSource, { withFileTypes: true })) {
    if (entry.name.toLowerCase() !== "source") {
      const sourcePath = path.join(versionSource, entry.name);
      const targetPath = path.join(versionDestination, entry.name);
      if (entry.isDirectory()) await copyDirectory(sourcePath, targetPath);
      else if (entry.isFile()) await fs.copyFile(sourcePath, targetPath);
    }
  }

  for (const file of filesToCopy) {
    const source = path.join(projectRoot, file);
    if (await exists(source)) {
      await fs.copyFile(source, path.join(destination, file));
    }
  }

  console.log(`Deployed Tie to Tree to: ${destination}`);
}

deploy().catch((error) => {
  console.error(`Deploy failed: ${error.message}`);
  process.exitCode = 1;
});
