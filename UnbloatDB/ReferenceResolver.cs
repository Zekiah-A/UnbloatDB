using UnbloatDB.Attributes;

namespace UnbloatDB;

[ReferenceResolver]
public partial class ReferenceResolver
{
    private readonly Dictionary<Type, Dictionary<string, Type>> referenceMap; // Group.Property -> Group
    private readonly Dictionary<Type, Dictionary<string, Type>> referencersMap; // Group <- Group.Property
    private readonly Database database;
    
    public ReferenceResolver(Database db)
    {
        referenceMap = new Dictionary<Type, Dictionary<string, Type>>();
        referencersMap = new Dictionary<Type, Dictionary<string, Type>>();
        database = db;
        BuildReferenceMaps();
    }

    partial void BuildReferenceMaps();
    
    // Helpers for navigating the reference maps
    public RecordStructure<TGroup> GetReferencing<TGroup>(Database database, string forProperty)
    {
        
    }

    public IEnumerable<RecordStructure<TGroup>> GetAllReferencing<TGroup>(Database database)
    {
        
    }

    public RecordStructure<TGroup> GetReferencers<TGroup>(Database database, string forProperty)
    {
        
    }
    
    public IEnumerable<RecordStructure<TGroup>> GetAllReferencers<TGroup>(Database database)
    {
        
    }
}