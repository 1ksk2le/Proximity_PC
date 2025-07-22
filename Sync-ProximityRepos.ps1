# Sync-ProximityRepos.ps1
# Run this from your Proximity_PC repo folder

$mainRepoPath = "C:\Users\doguk\Desktop\Proximity"  # Adjust path if needed

# Stage and commit all changes in Proximity_PC
git add -A
git commit -m "Sync changes from Proximity_PC"

# Push changes to Proximity_PC origin
git push origin master

# Push changes to Proximity (mobile) repo
git remote add mobile $mainRepoPath 2>$null
git push mobile master

Write-Host "Changes pushed to both Proximity_PC and Proximity (mobile) repos."