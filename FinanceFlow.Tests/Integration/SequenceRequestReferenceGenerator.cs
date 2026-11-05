using FinanceFlow.Services;

namespace FinanceFlow.Tests.Integration
{
    public class SequenceRequestReferenceGenerator(params string[] referenceNumbers) : IRequestReferenceGenerator
    {
        private readonly Queue<string> _referenceNumbers = new(referenceNumbers);

        public string Generate()
        {
            return _referenceNumbers.Dequeue();
        }
    }
}
