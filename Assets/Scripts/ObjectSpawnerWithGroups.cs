using System.Collections.Generic;

public class ObjectSpawnerWithGroups : ObjectSpawner
{
    protected override void CreateBox()
    {
        if (!CheckCurrentGroupCompleted() && !hasToCompleteGroup && currentBox != null)
        {
            List<int> currentGroupTemp = FindSmallerGroup();

            if (currentGroupTemp != null)
            {
                List<Figure> currentBoxFiguresCopy = new List<Figure>(currentBoxFigures);

                for (int k = currentBoxFiguresCopy.Count - 1; k >= currentBoxFiguresCopy.Count - currentGroupInstantiatedFigures; k--)
                {
                    Figure currentFigure = currentBoxFiguresCopy[k];
                    currentFigure.gameObject.SetActive(false);
                    currentFigure.gameObject.transform.SetParent(null);
                    int figureIndex = int.Parse(currentFigure.gameObject.name[1] + "") - 1;
                    intantiatedObjects[figureIndex].Add(currentFigure);
                    currentGroup[figureIndex]++;
                    currentBoxFigures.RemoveAt(currentBoxFigures.Count - 1);
                }

                SetCurrentGroup(currentGroupTemp);
                return;
            }
            else
            {
                hasToCompleteGroup = true;
            }
        }

        base.CreateBox();
    }
}
